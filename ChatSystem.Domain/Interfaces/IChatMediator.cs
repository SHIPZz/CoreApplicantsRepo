using ChatSystem.Domain.Entities;
using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using ChatSystem.Domain.Enums;

namespace ChatSystem.Domain.Interfaces
{
    public interface IChatMediator
    {
        Task SendMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
        Task SendNotificationAsync(EventType eventType, object data, CancellationToken cancellationToken = default);
        IObservable<ChatMessage> OnMessageReceived { get; }
        IObservable<(EventType, object)> OnEventReceived { get; }
        void SubscribeToMessages(IObserver<ChatMessage> observer);
        void SubscribeToEvents(IObserver<(EventType, object)> observer);
    }
} 