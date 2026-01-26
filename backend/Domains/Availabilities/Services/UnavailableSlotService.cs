using backend.Database;
using backend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Domains.Availabilities;

public class UnavailableSlotService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UnavailableSlotService> _logger;

    public UnavailableSlotService(AppDbContext dbContext, ILogger<UnavailableSlotService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<UnavailableSlot> BlockTimeAsync(Guid teacherId, DateTime blockedDate, TimeOnly blockedStartTime, TimeOnly blockedEndTime, string? reason = null)
    {
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

        if (block.TeacherId != teacherId)
        {
            throw new InvalidOperationException("You do not have permission to remove this block");
        }

        _dbContext.UnavailableSlots.Remove(block);
        await _dbContext.SaveChangesAsync();

        return true;
    }
}
