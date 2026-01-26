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

    public async Task<Availability> CreateAvailabilityAsync(Guid teacherId, int dayOfWeek, TimeOnly startTime, TimeOnly endTime, bool isRecurring = true)
    {
        // Validate that EndTime is after StartTime
        if (endTime <= startTime)
        {
            throw new ArgumentException("EndTime must be after StartTime");
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
        return await _dbContext.Availabilities
            .Where(a => a.TeacherId == teacherId)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<bool> TeacherExistsAsync(Guid teacherId)
    {
        return await _dbContext.Users
            .AnyAsync(u => u.Id == teacherId && u.Profile == ProfileType.Teacher);
    }
}
