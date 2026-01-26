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
