using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Enums;
using ChatSystem.Domain.Interfaces;
using System.Collections.Concurrent;

namespace ChatSystem.Infrastructure.Repositories
{
    public class ChatMessageRepository : IChatMessageRepository
    {
        private readonly ConcurrentQueue<ChatMessage> _messages = new();
        private readonly ConcurrentDictionary<string, DateTime> _processedMessages = new();
        private readonly Lock _lockObject = new();

        public async Task<IEnumerable<ChatMessage>> GetMessagesByTypeAsync(ChatType type, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(10, cancellationToken);
                return _messages.Where(m => m.Type == type).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"@@@Error in GetMessagesByTypeAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<ChatMessage>> GetMessagesBySenderAsync(string sender, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(10, cancellationToken);
                return _messages.Where(m => m.Sender == sender).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"@@@Error in GetMessagesBySenderAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<ChatMessage>> GetRecentMessagesAsync(int count, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(10, cancellationToken);
                return _messages.TakeLast(count).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"@@@Error in GetRecentMessagesAsync: {ex.Message}");
                throw;
            }
        }

        public async Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(10, cancellationToken);
                _messages.Enqueue(message);
                
                lock (_lockObject)
                {
                    _processedMessages[CreateMessageKey(message)] = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"@@@Error in AddMessageAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsDuplicateMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(10, cancellationToken);
                var key = CreateMessageKey(message);
                
                lock (_lockObject)
                {
                    return _processedMessages.ContainsKey(key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"@@@Error in IsDuplicateMessageAsync: {ex.Message}");
                throw;
            }
        }

        public async Task ClearOldMessagesAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(10, cancellationToken);
                var cutoffTime = DateTime.UtcNow.Subtract(olderThan);
                
                lock (_lockObject)
                {
                    var keysToRemove = _processedMessages
                        .Where(kvp => kvp.Value < cutoffTime)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in keysToRemove)
                    {
                        _processedMessages.TryRemove(key, out _);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"@@@Error in ClearOldMessagesAsync: {ex.Message}");
                throw;
            }
        }

        private string CreateMessageKey(ChatMessage message)
        {
            return $"{message.Type}_{message.Sender}_{message.Content}";
        }
    }
} 