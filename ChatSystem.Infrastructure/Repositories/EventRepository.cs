using ChatSystem.Domain.Enums;
using ChatSystem.Domain.Interfaces;
using System.Collections.Concurrent;

namespace ChatSystem.Infrastructure.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly ConcurrentQueue<(EventType, object, DateTime)> _events = new();

        public async Task<IEnumerable<(EventType, object)>> GetEventsByTypeAsync(EventType type, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(10, cancellationToken);
                return _events.Where(e => e.Item1 == type).Select(e => (e.Item1, e.Item2)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"@@@Error in GetEventsByTypeAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<(EventType, object)>> GetRecentEventsAsync(int count, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(10, cancellationToken);
                return _events.TakeLast(count).Select(e => (e.Item1, e.Item2)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"@@@Error in GetRecentEventsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task AddEventAsync(EventType eventType, object data, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(10, cancellationToken);
                _events.Enqueue((eventType, data, DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"@@@Error in AddEventAsync: {ex.Message}");
                throw;
            }
        }

        public async Task ClearOldEventsAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(10, cancellationToken);
                var cutoffTime = DateTime.UtcNow.Subtract(olderThan);
                
                var eventsToKeep = _events.Where(e => e.Item3 >= cutoffTime).ToList();
                _events.Clear();
                
                foreach (var evt in eventsToKeep)
                {
                    _events.Enqueue(evt);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"@@@Error in ClearOldEventsAsync: {ex.Message}");
                throw;
            }
        }
    }
} 