using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Enums;
using ChatSystem.Domain.Interfaces;
using System.Reactive.Subjects;

namespace ChatSystem.Application.Services
{
    public class ChatMediator : IChatMediator
    {
        private readonly IChatNetwork _network;
        private readonly IChatMessageRepository _messageRepository;
        private readonly IEventRepository _eventRepository;
        private readonly Subject<ChatMessage> _messageSubject = new();
        private readonly Subject<(EventType, object)> _eventSubject = new();
        private readonly HashSet<string> _recentlySentMessages = new();

        public ChatMediator(IChatNetwork network, IChatMessageRepository messageRepository, IEventRepository eventRepository)
        {
            _network = network;
            _messageRepository = messageRepository;
            _eventRepository = eventRepository;
            SubscribeToNetworkEvents();
        }

        public IObservable<ChatMessage> OnMessageReceived => _messageSubject;
        public IObservable<(EventType, object)> OnEventReceived => _eventSubject;

        public async Task SendMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (await _messageRepository.IsDuplicateMessageAsync(message, cancellationToken))
                {
                    LogDuplicateMessage(message);
                    return;
                }

                var messageKey = CreateMessageKey(message);
                _recentlySentMessages.Add(messageKey);
                
                await _messageRepository.AddMessageAsync(message, cancellationToken);
                await CleanupOldMessagesIfNeeded(cancellationToken);
                await _network.SendMessageAsync(message, cancellationToken);
                
                await Task.Delay(100, cancellationToken);
                _recentlySentMessages.Remove(messageKey);
            }
            catch (Exception ex)
            {
                HandleError("SendMessageAsync", ex);
                throw;
            }
        }

        public async Task SendNotificationAsync(EventType eventType, object data, CancellationToken cancellationToken = default)
        {
            try
            {
                await _eventRepository.AddEventAsync(eventType, data, cancellationToken);
                await _network.RaiseEventAsync(eventType, data, cancellationToken);
            }
            catch (Exception ex)
            {
                HandleError("SendNotificationAsync", ex);
                throw;
            }
        }

        public void SubscribeToMessages(IObserver<ChatMessage> observer)
        {
            _messageSubject.Subscribe(observer);
        }

        public void SubscribeToEvents(IObserver<(EventType, object)> observer)
        {
            _eventSubject.Subscribe(observer);
        }

        private void SubscribeToNetworkEvents()
        {
            _network.OnMessageReceived.Subscribe(ProcessIncomingMessage);
            _network.OnEventReceived.Subscribe(ProcessIncomingEvent);
        }

        private void ProcessIncomingMessage(ChatMessage message)
        {
            try
            {
                var messageKey = CreateMessageKey(message);
                if (_recentlySentMessages.Contains(messageKey))
                {
                    Console.WriteLine($"Mediator: Ignoring recently sent message: {message}");
                    return;
                }
                
                Console.WriteLine($"Mediator: Processing message from {message.Sender}");
                _messageSubject.OnNext(message);
            }
            catch (Exception ex)
            {
                HandleError("ProcessIncomingMessage", ex);
            }
        }

        private void ProcessIncomingEvent((EventType, object) eventData)
        {
            try
            {
                _eventSubject.OnNext(eventData);
            }
            catch (Exception ex)
            {
                HandleError("ProcessIncomingEvent", ex);
            }
        }

        private void LogDuplicateMessage(ChatMessage message)
        {
            Console.WriteLine($"Authority: Duplicate message filtered - {message}");
        }

        private async Task CleanupOldMessagesIfNeeded(CancellationToken cancellationToken)
        {
            try
            {
                await _messageRepository.ClearOldMessagesAsync(TimeSpan.FromMinutes(5), cancellationToken);
                await _eventRepository.ClearOldEventsAsync(TimeSpan.FromMinutes(5), cancellationToken);
            }
            catch (Exception ex)
            {
                HandleError("CleanupOldMessagesIfNeeded", ex);
            }
        }

        private void HandleError(string methodName, Exception ex)
        {
            Console.WriteLine($"@@@Error in {methodName}: {ex.Message}");
        }
        
        private string CreateMessageKey(ChatMessage message)
        {
            return $"{message.Sender}:{message.Content}:{message.Type}";
        }
    }
} 