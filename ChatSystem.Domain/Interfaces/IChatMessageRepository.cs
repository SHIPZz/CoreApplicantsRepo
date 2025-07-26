using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Enums;

namespace ChatSystem.Domain.Interfaces
{
    public interface IChatMessageRepository
    {
        Task<IEnumerable<ChatMessage>> GetMessagesByTypeAsync(ChatType type, CancellationToken cancellationToken = default);
        Task<IEnumerable<ChatMessage>> GetMessagesBySenderAsync(string sender, CancellationToken cancellationToken = default);
        Task<IEnumerable<ChatMessage>> GetRecentMessagesAsync(int count, CancellationToken cancellationToken = default);
        Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
        Task<bool> IsDuplicateMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
        Task ClearOldMessagesAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
    }
} 