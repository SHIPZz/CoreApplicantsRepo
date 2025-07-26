using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Enums;

namespace ChatSystem.Domain.Interfaces
{
    public interface IChatNetwork
    {
        Task SendMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
        Task RaiseEventAsync(EventType eventType, object data, CancellationToken cancellationToken = default);
        IObservable<ChatMessage> OnMessageReceived { get; }
        IObservable<(EventType, object)> OnEventReceived { get; }
        void AddClient(string clientId);
        void RemoveClient(string clientId);
        void SimulateDisconnect();
        void SimulateReconnect();
    }
} 