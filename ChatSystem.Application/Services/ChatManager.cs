using System.Reactive.Linq;
using System.Reactive.Subjects;
using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Enums;
using ChatSystem.Domain.Interfaces;

namespace ChatSystem.Application.Services
{
    public class ChatManager : IChatManager
    {
        private readonly IChatMediator _mediator;
        private readonly Subject<ChatMessage> _messages = new();
        private readonly Subject<(EventType, object)> _events = new();
        private readonly string _clientId;
        private MessageObserver? _messageObserver;
        private EventObserver? _eventObserver;
        private bool _disposed = false;

        public ChatManager(IChatMediator mediator, string clientId)
        {
            _mediator = mediator;
            _clientId = clientId;
        }

        public IObservable<ChatMessage> Messages => _messages;
        public IObservable<(EventType, object)> Events => _events;
        public string ClientId => _clientId;

        public void Initialize()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChatManager));

            _messageObserver = new MessageObserver(_messages);
            _eventObserver = new EventObserver(_events);
            
            _mediator.SubscribeToMessages(_messageObserver);
            _mediator.SubscribeToEvents(_eventObserver);
        }

        public async Task SendChatMessageAsync(ChatType type, string content, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            try
            {
                var message = CreateChatMessage(type, content);
                await SendMessageWithRetryAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                HandleCriticalError("SendChatMessageAsync", ex);
                throw;
            }
        }

        public async Task SendNotificationAsync(EventType eventType, object data, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            try
            {
                await _mediator.SendNotificationAsync(eventType, data, cancellationToken);
            }
            catch (Exception ex)
            {
                HandleError("SendNotificationAsync", ex);
                throw;
            }
        }

        public IObservable<ChatMessage> GetMessagesByType(ChatType type)
        {
            EnsureNotDisposed();
            return _messages.Where(msg => msg.Type == type);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _messageObserver?.Dispose();
                _eventObserver?.Dispose();
                _messages?.Dispose();
                _events?.Dispose();
                _disposed = true;
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChatManager));
        }

        private ChatMessage CreateChatMessage(ChatType type, string content)
        {
            return new ChatMessage(type, _clientId, content);
        }

        private async Task SendMessageWithRetryAsync(ChatMessage message, CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine($"@@@Sending message: {message}");
                await _mediator.SendMessageAsync(message, cancellationToken);
                Console.WriteLine($"@@@Message sent successfully: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"@@@Retry triggered for message {message}: {ex.Message}");
                HandleNetworkError(ex);
                await WaitForRetryAsync(cancellationToken);
                await _mediator.SendMessageAsync(message, cancellationToken);
            }
        }

        private void HandleNetworkError(Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }

        private async Task WaitForRetryAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(500, cancellationToken);
        }

        private void HandleError(string methodName, Exception ex)
        {
            Console.WriteLine($"Error in {methodName}: {ex.Message}");
        }

        private void HandleCriticalError(string methodName, Exception ex)
        {
            Console.WriteLine($"Critical error in {methodName}: {ex.Message}");
        }
    }

    internal class MessageObserver : IObserver<ChatMessage>, IDisposable
    {
        private readonly Subject<ChatMessage> _subject;
        private IDisposable? _subscription;

        public MessageObserver(Subject<ChatMessage> subject) 
        { 
            _subject = subject; 
        }

        public void OnCompleted() => _subject.OnCompleted();
        public void OnError(Exception error) => _subject.OnError(error);
        public void OnNext(ChatMessage value) => _subject.OnNext(value);

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }

    internal class EventObserver : IObserver<(EventType, object)>, IDisposable
    {
        private readonly Subject<(EventType, object)> _subject;
        private IDisposable? _subscription;

        public EventObserver(Subject<(EventType, object)> subject) 
        { 
            _subject = subject; 
        }

        public void OnCompleted() => _subject.OnCompleted();
        public void OnError(Exception error) => _subject.OnError(error);
        public void OnNext((EventType, object) value) => _subject.OnNext(value);

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }
} 