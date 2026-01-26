using backend.Database;
using backend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Availabilities;

public class AvailabilityQueryService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AvailabilityQueryService> _logger;

    public AvailabilityQueryService(AppDbContext dbContext, ILogger<AvailabilityQueryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
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
