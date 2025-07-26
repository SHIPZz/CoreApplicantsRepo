using ChatSystem.Domain.Enums;

namespace ChatSystem.Domain.Interfaces
{
    public interface IEventRepository
    {
        Task<IEnumerable<(EventType, object)>> GetEventsByTypeAsync(EventType type, CancellationToken cancellationToken = default);
        Task<IEnumerable<(EventType, object)>> GetRecentEventsAsync(int count, CancellationToken cancellationToken = default);
        Task AddEventAsync(EventType eventType, object data, CancellationToken cancellationToken = default);
        Task ClearOldEventsAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
        Task ClearAllEventsAsync(CancellationToken cancellationToken = default);
    }
} 