using Microsoft.EntityFrameworkCore;
using Fleet.Shared.Messaging.Outbox;

namespace Fleet.VehicleService.Infrastructure.Persistence.Outbox;

public class VehicleOutboxStore : IOutboxStore
{
    private readonly VehicleDbContext _context;

    public VehicleOutboxStore(VehicleDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 20, CancellationToken ct = default)
    {
        return await _context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.Error == null)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken ct = default)
    {
        await _context.OutboxMessages
            .Where(m => m.Id == messageId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.ProcessedAt, DateTimeOffset.UtcNow)
                .SetProperty(m => m.Error, (string?)null), ct);
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken ct = default)
    {
        await _context.OutboxMessages
            .Where(m => m.Id == messageId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Error, error), ct);
    }

    public async Task SaveAsync(OutboxMessage message, CancellationToken ct = default)
    {
        await _context.OutboxMessages.AddAsync(message, ct);
        await _context.SaveChangesAsync(ct);
    }
}