using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Enums;
using ChatSystem.Domain.Interfaces;
using System.Reactive.Subjects;

namespace ChatSystem.Infrastructure.Network
{
    public class MockChatNetwork : IChatNetwork
    {
        private readonly Subject<ChatMessage> _messageSubject = new();
        private readonly Subject<(EventType, object)> _eventSubject = new();
        private readonly List<string> _clients = new();
        private readonly List<string> _processedMessages = new();
        private readonly Lock _lockObject = new();
        private readonly int _latencyMs;
        private bool _isConnected = true;

        public MockChatNetwork(int latencyMs = 100)
        {
            _latencyMs = latencyMs;
        }

        public IObservable<ChatMessage> OnMessageReceived => _messageSubject;
        public IObservable<(EventType, object)> OnEventReceived => _eventSubject;

        public async Task SendMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateConnection();
                if (IsDuplicateMessage(message))
                {
                    LogDuplicateMessage(message);
                    return;
                }

                AddMessageToProcessedList(message);
                CleanupOldMessagesIfNeeded();
                await SimulateNetworkLatencyAsync(cancellationToken);
                BroadcastMessageIfClientConnected(message);
            }
            catch (Exception ex)
            {
                HandleError("SendMessageAsync", ex);
                throw;
            }
        }

        public async Task RaiseEventAsync(EventType eventType, object data, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateConnection();
                await SimulateNetworkLatencyAsync(cancellationToken);
                BroadcastEvent(eventType, data);
            }
            catch (Exception ex)
            {
                HandleError("RaiseEventAsync", ex);
                throw;
            }
        }

        public void AddClient(string clientId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (!_clients.Contains(clientId))
                    {
                        _clients.Add(clientId);
                        LogClientJoined(clientId);
                        NotifyPlayerJoined(clientId);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError("AddClient", ex);
            }
        }

        public void RemoveClient(string clientId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_clients.Remove(clientId))
                    {
                        LogClientLeft(clientId);
                        NotifyPlayerLeft(clientId);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError("RemoveClient", ex);
            }
        }

        public void SimulateDisconnect()
        {
            _isConnected = false;
        }

        public void SimulateReconnect()
        {
            _isConnected = true;
        }

        private void ValidateConnection()
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Disconnected");
            }
        }

        private bool IsDuplicateMessage(ChatMessage message)
        {
            lock (_lockObject)
            {
                var key = CreateMessageKey(message);
                return _processedMessages.Contains(key);
            }
        }

        private string CreateMessageKey(ChatMessage message)
        {
            return $"{message.Sender}:{message.Content}:{message.Type}";
        }

        private void LogDuplicateMessage(ChatMessage message)
        {
            Console.WriteLine($"Authority: Duplicate message filtered - {message}");
        }

        private void AddMessageToProcessedList(ChatMessage message)
        {
            lock (_lockObject)
            {
                var key = CreateMessageKey(message);
                _processedMessages.Add(key);
            }
        }

        private void CleanupOldMessagesIfNeeded()
        {
            lock (_lockObject)
            {
                if (_processedMessages.Count > 100)
                {
                    _processedMessages.RemoveRange(0, 50);
                }
            }
        }

        private async Task SimulateNetworkLatencyAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(_latencyMs, cancellationToken);
        }

        private void BroadcastMessageIfClientConnected(ChatMessage message)
        {
            lock (_lockObject)
            {
                LogMessageBroadcast(message);
                _messageSubject.OnNext(message);
            }
        }

        private void BroadcastEvent(EventType eventType, object data)
        {
            lock (_lockObject)
            {
                LogEventBroadcast(eventType, data);
                _eventSubject.OnNext((eventType, data));
            }
        }

        private void LogClientJoined(string clientId)
        {
            Console.WriteLine($"Client {clientId} joined. Total clients: {_clients.Count}");
        }

        private void LogClientLeft(string clientId)
        {
            Console.WriteLine($"Client {clientId} left. Total clients: {_clients.Count}");
        }

        private void NotifyPlayerJoined(string clientId)
        {
            _eventSubject.OnNext((EventType.PlayerJoined, clientId));
        }

        private void NotifyPlayerLeft(string clientId)
        {
            _eventSubject.OnNext((EventType.PlayerLeft, clientId));
        }

        private void LogMessageBroadcast(ChatMessage message)
        {
            Console.WriteLine($"Broadcasting message to {_clients.Count} clients: {message}");
        }

        private void LogEventBroadcast(EventType eventType, object data)
        {
            Console.WriteLine($"Broadcasting event {eventType} to {_clients.Count} clients: {data}");
        }

        private void HandleError(string methodName, Exception ex)
        {
            Console.WriteLine($"Error in {methodName}: {ex.Message}");
        }
    }
} 