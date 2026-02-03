using backend.Database;
using backend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Availabilities;

public class AvailabilityService
{
    private readonly AppDbContext _dbContext;
    private readonly AvailabilityQueryService _queryService;
    private readonly UnavailableSlotService _unavailableService;

    public AvailabilityService(AppDbContext dbContext,
        AvailabilityQueryService queryService,
        UnavailableSlotService unavailableService)
    {
        _dbContext = dbContext;
        _queryService = queryService;
        _unavailableService = unavailableService;
    }

    public async Task<Availability> CreateAvailabilityAsync(Guid teacherId, int dayOfWeek, TimeOnly startTime, TimeOnly endTime, bool isRecurring = true, DateOnly? availabilityDate = null)
    {
        if (!isRecurring && !availabilityDate.HasValue)
        {
            throw new ArgumentException("Non-recurring availability must have an availability date");
        }

        if (isRecurring && availabilityDate.HasValue)
        {
            throw new ArgumentException("Recurring availability should not have a specific date");
        }

        if (endTime <= startTime)
        {
            throw new ArgumentException("EndTime must be after StartTime");
        }

        if (!isRecurring && availabilityDate.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            
            if (availabilityDate.Value < today)
            {
                throw new ArgumentException("AvailabilityDate must be today or in the future");
            }

            if ((int)availabilityDate.Value.DayOfWeek != dayOfWeek)
            {
                throw new ArgumentException("DayOfWeek does not match AvailabilityDate");
            }
        }

        var existingAvailabilities = await _dbContext.Availabilities
            .Where(a => a.TeacherId == teacherId && a.DayOfWeek == dayOfWeek)
            .ToListAsync();

        if (isRecurring)
        {
            foreach (var existing in existingAvailabilities)
            {
                if (startTime < existing.EndTime && endTime > existing.StartTime)
                {
                    if (existing.IsRecurring)
                    {
                        throw new ArgumentException(
                            $"A recurring availability already exists for this time slot ({existing.StartTime:HH:mm} - {existing.EndTime:HH:mm})");
                    }
                    else
                    {
                        _dbContext.Availabilities.Remove(existing);
                    }
                }
            }
        }
        else
        {
            foreach (var existing in existingAvailabilities)
            {
                if (startTime < existing.EndTime && endTime > existing.StartTime)
                {
                    if (existing.IsRecurring)
                    {
                        throw new ArgumentException(
                            $"A recurring availability already exists for this time slot ({existing.StartTime:HH:mm} - {existing.EndTime:HH:mm}). No need to create a one-time availability.");
                    }
                    
                    if (availabilityDate == existing.AvailabilityDate)
                    {
                        throw new ArgumentException(
                            $"Availability overlaps with existing slot ({existing.StartTime:HH:mm} - {existing.EndTime:HH:mm}) on the same day");
                    }
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
        return await _queryService.GetTeacherAvailabilitiesAsync(teacherId);
    }

    public async Task<bool> TeacherExistsAsync(Guid teacherId)
    {
        return await _queryService.TeacherExistsAsync(teacherId);
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
        return await _unavailableService.BlockTimeAsync(teacherId, blockedDate, blockedStartTime, blockedEndTime, reason);
    }

    public async Task<bool> RemoveBlockAsync(Guid blockId, Guid teacherId)
    {
        return await _unavailableService.RemoveBlockAsync(blockId, teacherId);
    }

    public async Task<List<UnavailableSlot>> GetTeacherUnavailableSlotsAsync(Guid teacherId)
    {
        return await _queryService.GetTeacherUnavailableSlotsAsync(teacherId);
    }
}
