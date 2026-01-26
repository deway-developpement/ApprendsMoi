using backend.Database;
using backend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Availabilities;

public class AvailabilityService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(AppDbContext dbContext, ILogger<AvailabilityService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Availability> CreateAvailabilityAsync(Guid teacherId, int dayOfWeek, TimeOnly startTime, TimeOnly endTime, bool isRecurring = true, DateOnly? availabilityDate = null)
    {
        // Validate that EndTime is after StartTime
        if (endTime <= startTime)
        {
            throw new ArgumentException("EndTime must be after StartTime");
        }

        // For non-recurring, calculate date if not provided
        if (!isRecurring)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            
            if (!availabilityDate.HasValue)
            {
                // Calculate the next future day with the matching dayOfWeek
                var daysAhead = dayOfWeek - (int)today.DayOfWeek;
                if (daysAhead <= 0)
                {
                    daysAhead += 7; // Go to next week if day has passed
                }
                availabilityDate = today.AddDays(daysAhead);
            }
            else if (availabilityDate.Value < today)
            {
                throw new ArgumentException("AvailabilityDate must be today or in the future");
            }

            // Ensure dayOfWeek matches provided date
            if ((int)availabilityDate.Value.DayOfWeek != dayOfWeek)
            {
                throw new ArgumentException("DayOfWeek does not match AvailabilityDate");
            }
        }

        // Check for overlapping availabilities on the same day
        var existingAvailabilities = await _dbContext.Availabilities
            .Where(a => a.TeacherId == teacherId && a.DayOfWeek == dayOfWeek)
            .ToListAsync();

        if (isRecurring)
        {
            // When adding a recurring availability, handle overlapping non-recurring availabilities
            foreach (var existing in existingAvailabilities)
            {
                // Check if the new time range overlaps with existing availability
                // Overlap occurs when: newStart < existingEnd AND newEnd > existingStart
                if (startTime < existing.EndTime && endTime > existing.StartTime)
                {
                    if (existing.IsRecurring)
                    {
                        // Recurring overlaps with recurring - reject
                        throw new ArgumentException(
                            $"Availability overlaps with existing recurring slot ({existing.StartTime:HH:mm:ss} - {existing.EndTime:HH:mm:ss}) on the same day");
                    }
                    else
                    {
                        // Recurring overlaps with non-recurring - adjust the non-recurring
                        // Case 1: Recurring completely contains non-recurring
                        if (startTime <= existing.StartTime && endTime >= existing.EndTime)
                        {
                            // Delete the non-recurring availability
                            _dbContext.Availabilities.Remove(existing);
                        }
                        // Case 2: Recurring is in the middle of non-recurring - split it
                        else if (startTime > existing.StartTime && endTime < existing.EndTime)
                        {
                            // Create a new slot from recurring.end to existing.end
                            var newSlot = new Availability
                            {
                                TeacherId = teacherId,
                                DayOfWeek = dayOfWeek,
                                AvailabilityDate = existing.AvailabilityDate,
                                StartTime = endTime,
                                EndTime = existing.EndTime,
                                IsRecurring = false
                            };
                            _dbContext.Availabilities.Add(newSlot);
                            
                            // Trim existing to end at recurring.start
                            existing.EndTime = startTime;
                        }
                        // Case 3: Recurring overlaps from the left
                        else if (startTime <= existing.StartTime && endTime < existing.EndTime)
                        {
                            // Trim non-recurring to start at recurring.end
                            existing.StartTime = endTime;
                        }
                        // Case 4: Recurring overlaps from the right
                        else if (startTime > existing.StartTime && endTime >= existing.EndTime)
                        {
                            // Trim non-recurring to end at recurring.start
                            existing.EndTime = startTime;
                        }
                    }
                }
            }
        }
        else
        {
            // Non-recurring availability - reject any overlaps
            foreach (var existing in existingAvailabilities)
            {
                if (startTime < existing.EndTime && endTime > existing.StartTime)
                {
                    throw new ArgumentException(
                        $"Availability overlaps with existing slot ({existing.StartTime:HH:mm:ss} - {existing.EndTime:HH:mm:ss}) on the same day");
                }
            }
        }

        var availability = new Availability
        {
            TeacherId = teacherId,
            DayOfWeek = dayOfWeek,
            AvailabilityDate = availabilityDate,
            StartTime = startTime,
            EndTime = endTime,
            IsRecurring = isRecurring
        };

        _dbContext.Availabilities.Add(availability);
        await _dbContext.SaveChangesAsync();

        return availability;
    }

    public async Task<List<Availability>> GetTeacherAvailabilitiesAsync(Guid teacherId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.Availabilities
            .Where(a => a.TeacherId == teacherId
                        && (a.IsRecurring || (a.AvailabilityDate != null && a.AvailabilityDate >= today)))
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<bool> TeacherExistsAsync(Guid teacherId)
    {
        return await _dbContext.Users
            .AnyAsync(u => u.Id == teacherId && u.Profile == ProfileType.Teacher);
    }

    public async Task<bool> DeleteAvailabilityAsync(Guid availabilityId, Guid teacherId)
    {
        var availability = await _dbContext.Availabilities
            .FirstOrDefaultAsync(a => a.Id == availabilityId);

        if (availability == null)
        {
            return false;
        }

        // Verify the availability belongs to the teacher
        if (availability.TeacherId != teacherId)
        {
            throw new InvalidOperationException("You do not have permission to delete this availability");
        }

        _dbContext.Availabilities.Remove(availability);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<UnavailableSlot> BlockTimeAsync(Guid teacherId, DateTime blockedDate, TimeOnly blockedStartTime, TimeOnly blockedEndTime, string? reason = null)
    {
        // Validate that EndTime is after StartTime
        if (blockedEndTime <= blockedStartTime)
        {
            throw new ArgumentException("Blocked end time must be after blocked start time");
        }

        var unavailableSlot = new UnavailableSlot
        {
            TeacherId = teacherId,
            BlockedDate = blockedDate.Date,
            BlockedStartTime = blockedStartTime,
            BlockedEndTime = blockedEndTime,
            Reason = reason
        };

        _dbContext.UnavailableSlots.Add(unavailableSlot);
        await _dbContext.SaveChangesAsync();

        return unavailableSlot;
    }

    public async Task<bool> RemoveBlockAsync(Guid blockId, Guid teacherId)
    {
        var block = await _dbContext.UnavailableSlots
            .FirstOrDefaultAsync(u => u.Id == blockId);

        if (block == null)
        {
            return false;
        }

        // Verify the block belongs to the teacher
        if (block.TeacherId != teacherId)
        {
            throw new InvalidOperationException("You do not have permission to remove this block");
        }

        _dbContext.UnavailableSlots.Remove(block);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<List<UnavailableSlot>> GetTeacherUnavailableSlotsAsync(Guid teacherId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.UnavailableSlots
            .Where(u => u.TeacherId == teacherId && DateOnly.FromDateTime(u.BlockedDate) >= today)
            .OrderBy(u => u.BlockedDate)
            .ThenBy(u => u.BlockedStartTime)
            .ToListAsync();
    }
}
