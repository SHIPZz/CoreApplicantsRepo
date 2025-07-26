using System.Reactive.Subjects;
using ChatSystem.Application.Services;
using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Enums;
using ChatSystem.Domain.Interfaces;
using Moq;
using NUnit.Framework;

namespace ChatSystem.Tests
{
    [TestFixture]
    public class ChatMediatorTests
    {
        private Mock<IChatNetwork> _mockNetwork;
        private Mock<IChatMessageRepository> _mockMessageRepository;
        private Mock<IEventRepository> _mockEventRepository;
        private ChatMediator _mediator;
        private Subject<ChatMessage> _messageSubject;
        private Subject<(EventType, object)> _eventSubject;

        [SetUp]
        public void SetUp()
        {
            _messageSubject = new Subject<ChatMessage>();
            _eventSubject = new Subject<(EventType, object)>();
            _mockNetwork = new Mock<IChatNetwork>();
            _mockMessageRepository = new Mock<IChatMessageRepository>();
            _mockEventRepository = new Mock<IEventRepository>();

            _mockNetwork.Setup(n => n.OnMessageReceived).Returns(_messageSubject);
            _mockNetwork.Setup(n => n.OnEventReceived).Returns(_eventSubject);

            _mediator = new ChatMediator(_mockNetwork.Object, _mockMessageRepository.Object, _mockEventRepository.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _messageSubject?.Dispose();
            _eventSubject?.Dispose();
        }

        [Test]
        public async Task SendMessageAsync_NonDuplicateMessage_SendsToNetwork()
        {
            var message = new ChatMessage(ChatType.Public, "Player1", "Test message");
            _mockMessageRepository.Setup(r => r.IsDuplicateMessageAsync(message, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            try
            {
                await _mediator.SendMessageAsync(message);

                _mockNetwork.Verify(n => n.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Once);
                _mockMessageRepository.Verify(r => r.AddMessageAsync(message, It.IsAny<CancellationToken>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }
        }

        [Test]
        public async Task SendMessageAsync_DuplicateMessage_DoesNotSendToNetwork()
        {
            var message = new ChatMessage(ChatType.Public, "Player1", "Test message");
            _mockMessageRepository.Setup(r => r.IsDuplicateMessageAsync(message, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            try
            {
                await _mediator.SendMessageAsync(message);

                _mockNetwork.Verify(n => n.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Never);
                _mockMessageRepository.Verify(r => r.AddMessageAsync(message, It.IsAny<CancellationToken>()), Times.Never);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }
        }

        [Test]
        public async Task SendNotificationAsync_SendsToNetworkAndRepository()
        {
            var eventType = EventType.MatchStart;
            var data = "Match started!";

            try
            {
                await _mediator.SendNotificationAsync(eventType, data);

                _mockNetwork.Verify(n => n.RaiseEventAsync(eventType, data, It.IsAny<CancellationToken>()), Times.Once);
                _mockEventRepository.Verify(r => r.AddEventAsync(eventType, data, It.IsAny<CancellationToken>()), Times.Once);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }
        }

        [Test]
        public void OnMessageReceived_NetworkMessageReceived_PropagatesToObservers()
        {
            var message = new ChatMessage(ChatType.Public, "Player1", "Test message");
            var receivedMessage = (ChatMessage)null;

            _mediator.OnMessageReceived.Subscribe(msg => receivedMessage = msg);

            try
            {
                _messageSubject.OnNext(message);

                Assert.That(receivedMessage, Is.EqualTo(message));
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }
        }

        [Test]
        public void OnEventReceived_NetworkEventReceived_PropagatesToObservers()
        {
            var eventData = (EventType.MatchStart, (object)"Match started!");
            var receivedEvent = ((EventType, object))default;

            _mediator.OnEventReceived.Subscribe(evt => receivedEvent = evt);

            try
            {
                _eventSubject.OnNext(eventData);

                Assert.That(receivedEvent, Is.EqualTo(eventData));
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }
        }
    }
} 