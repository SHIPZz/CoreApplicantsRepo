using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Enums;

namespace ChatSystem.Domain.Interfaces
{
    public interface IChatManager : IDisposable
    {
        IObservable<ChatMessage> Messages { get; }
        IObservable<(EventType, object)> Events { get; }
        string ClientId { get; }
        
        void Initialize();
        Task SendChatMessageAsync(ChatType type, string content, CancellationToken cancellationToken = default);
        Task SendNotificationAsync(EventType eventType, object data, CancellationToken cancellationToken = default);
        IObservable<ChatMessage> GetMessagesByType(ChatType type);
    }
} 