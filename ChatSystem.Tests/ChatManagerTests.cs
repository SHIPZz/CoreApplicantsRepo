using ChatSystem.Application.Services;
using ChatSystem.Application.Builders;
using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Enums;
using ChatSystem.Domain.Interfaces;
using Moq;
using NUnit.Framework;
using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ChatSystem.Tests
{
    [TestFixture]
    public class ChatManagerTests
    {
        private Mock<IChatMediator> _mockMediator;
        private ChatManager _manager;
        private Subject<ChatMessage> _messageSubject;
        private Subject<(EventType, object)> _eventSubject;

        [SetUp]
        public void SetUp()
        {
            _messageSubject = new Subject<ChatMessage>();
            _eventSubject = new Subject<(EventType, object)>();
            
            _mockMediator = new Mock<IChatMediator>();
            _mockMediator.Setup(m => m.SubscribeToMessages(It.IsAny<IObserver<ChatMessage>>()))
                        .Callback<IObserver<ChatMessage>>(observer => _messageSubject.Subscribe(observer));
            _mockMediator.Setup(m => m.SubscribeToEvents(It.IsAny<IObserver<(EventType, object)>>()))
                        .Callback<IObserver<(EventType, object)>>(observer => _eventSubject.Subscribe(observer));
            
            _manager = new ChatManager(_mockMediator.Object, "TestPlayer");
            _manager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _manager?.Dispose();
            _messageSubject?.Dispose();
            _eventSubject?.Dispose();
        }

        [Test]
        public async Task SendChatMessageAsync_CallsMediatorSend_WithCorrectMessage()
        {
            ChatMessage? receivedMessage = null;
            _manager.Messages.Subscribe(msg => receivedMessage = msg);

            _mockMediator.Setup(m => m.SendMessageAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask)
                        .Callback<ChatMessage, CancellationToken>((msg, token) => _messageSubject.OnNext(msg));

            try
            {
                await _manager.SendChatMessageAsync(ChatType.Public, "Hello");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            _mockMediator.Verify(m => m.SendMessageAsync(It.Is<ChatMessage>(msg => 
                msg.Type == ChatType.Public && 
                msg.Sender == "TestPlayer" && 
                msg.Content == "Hello"), It.IsAny<CancellationToken>()), Times.Once());
            
            Assert.That(receivedMessage, Is.Not.Null);
            Assert.That(receivedMessage!.Type, Is.EqualTo(ChatType.Public));
            Assert.That(receivedMessage.Sender, Is.EqualTo("TestPlayer"));
            Assert.That(receivedMessage.Content, Is.EqualTo("Hello"));
        }

        [Test]
        public async Task SendNotificationAsync_WithBuilder_CallsMediatorSend()
        {
            (EventType, object) receivedEvent = default;
            _manager.Events.Subscribe(ev => receivedEvent = ev);

            _mockMediator.Setup(m => m.SendNotificationAsync(It.IsAny<EventType>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask)
                        .Callback<EventType, object, CancellationToken>((type, data, token) => _eventSubject.OnNext((type, data)));

            var builder = new NotificationBuilder()
                .SetType(EventType.KillNotification)
                .SetMessage("Player1 killed Player2");
            var notification = builder.Build();

            try
            {
                await _manager.SendNotificationAsync(notification.Item1, notification.Item2);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            _mockMediator.Verify(m => m.SendNotificationAsync(EventType.KillNotification, "Player1 killed Player2", It.IsAny<CancellationToken>()), Times.Once());
            Assert.That(receivedEvent.Item1, Is.EqualTo(EventType.KillNotification));
            Assert.That(receivedEvent.Item2, Is.EqualTo("Player1 killed Player2"));
        }

        [Test]
        public async Task SendChatMessageAsync_OnException_RetriesAfterDelay()
        {
            _mockMediator.SetupSequence(m => m.SendMessageAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Network error"))
                        .Returns(Task.CompletedTask);

            try
            {
                await _manager.SendChatMessageAsync(ChatType.Team, "Retry test");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            _mockMediator.Verify(m => m.SendMessageAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public void GetMessagesByType_ReturnsFilteredMessages()
        {
            var publicMessages = new List<ChatMessage>();
            var teamMessages = new List<ChatMessage>();

            _manager.GetMessagesByType(ChatType.Public).Subscribe(msg => publicMessages.Add(msg));
            _manager.GetMessagesByType(ChatType.Team).Subscribe(msg => teamMessages.Add(msg));

            _messageSubject.OnNext(new ChatMessage(ChatType.Public, "Player1", "Public message"));
            _messageSubject.OnNext(new ChatMessage(ChatType.Team, "Player2", "Team message"));
            _messageSubject.OnNext(new ChatMessage(ChatType.Public, "Player3", "Another public"));

            Assert.That(publicMessages.Count, Is.EqualTo(2));
            Assert.That(teamMessages.Count, Is.EqualTo(1));
            Assert.That(publicMessages[0].Content, Is.EqualTo("Public message"));
            Assert.That(teamMessages[0].Content, Is.EqualTo("Team message"));
        }

        [Test]
        public void ClientId_ReturnsCorrectValue()
        {
            Assert.That(_manager.ClientId, Is.EqualTo("TestPlayer"));
        }

        [Test]
        public void Dispose_ThrowsObjectDisposedException_WhenUsedAfterDispose()
        {
            _manager.Dispose();

            Assert.That(() => _manager.SendChatMessageAsync(ChatType.Public, "test"), 
                Throws.TypeOf<ObjectDisposedException>());
            Assert.That(() => _manager.SendNotificationAsync(EventType.MatchStart, "test"), 
                Throws.TypeOf<ObjectDisposedException>());
            Assert.That(() => _manager.GetMessagesByType(ChatType.Public), 
                Throws.TypeOf<ObjectDisposedException>());
        }
    }
}