using backend.Database;
using backend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Availabilities.Services;

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

        // For non-recurring, date is required and must be today or future
        if (!isRecurring)
        {
            if (!availabilityDate.HasValue)
            {
                throw new ArgumentException("AvailabilityDate is required when the slot is not recurring");
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (availabilityDate.Value < today)
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

        foreach (var existing in existingAvailabilities)
        {
            // Check if the new time range overlaps with existing availability
            // Overlap occurs when: newStart < existingEnd AND newEnd > existingStart
            if (startTime < existing.EndTime && endTime > existing.StartTime)
            {
                throw new ArgumentException(
                    $"Availability overlaps with existing slot ({existing.StartTime:HH:mm:ss} - {existing.EndTime:HH:mm:ss}) on the same day");
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
