using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Enums;
using ChatSystem.Infrastructure.Repositories;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChatSystem.Tests
{
    [TestFixture]
    public class RepositoryTests
    {
        private ChatMessageRepository _messageRepository;
        private EventRepository _eventRepository;

        [SetUp]
        public void SetUp()
        {
            _messageRepository = new ChatMessageRepository();
            _eventRepository = new EventRepository();
        }

        [Test]
        public async Task AddMessageAsync_MessageStored()
        {
            var message = new ChatMessage(ChatType.Public, "Player1", "Test message");

            try
            {
                await _messageRepository.AddMessageAsync(message);
                var messages = await _messageRepository.GetMessagesByTypeAsync(ChatType.Public);
                
                Assert.That(messages.Count(), Is.EqualTo(1));
                Assert.That(messages.First(), Is.EqualTo(message));
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }
        }

        [Test]
        public async Task IsDuplicateMessageAsync_ReturnsTrue_ForDuplicate()
        {
            var message = new ChatMessage(ChatType.Public, "Player1", "Test message");

            try
            {
                await _messageRepository.AddMessageAsync(message);
                var isDuplicate = await _messageRepository.IsDuplicateMessageAsync(message);
                
                Assert.That(isDuplicate, Is.True);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }
        }

        [Test]
        public async Task IsDuplicateMessageAsync_ReturnsFalse_ForNewMessage()
        {
            var message = new ChatMessage(ChatType.Public, "Player1", "Test message");

            try
            {
                var isDuplicate = await _messageRepository.IsDuplicateMessageAsync(message);
                
                Assert.That(isDuplicate, Is.False);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }
        }

        [Test]
        public async Task GetMessagesBySenderAsync_ReturnsCorrectMessages()
        {
            var message1 = new ChatMessage(ChatType.Public, "Player1", "Message 1");
            var message2 = new ChatMessage(ChatType.Team, "Player1", "Message 2");
            var message3 = new ChatMessage(ChatType.Public, "Player2", "Message 3");

            try
            {
                await _messageRepository.AddMessageAsync(message1);
                await _messageRepository.AddMessageAsync(message2);
                await _messageRepository.AddMessageAsync(message3);

                var player1Messages = await _messageRepository.GetMessagesBySenderAsync("Player1");
                
                Assert.That(player1Messages.Count(), Is.EqualTo(2));
                Assert.That(player1Messages.Any(m => m.Content == "Message 1"), Is.True);
                Assert.That(player1Messages.Any(m => m.Content == "Message 2"), Is.True);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }
        }

        [Test]
        public async Task AddEventAsync_EventStored()
        {
            try
            {
                await _eventRepository.AddEventAsync(EventType.MatchStart, "Match started!");
                var events = await _eventRepository.GetEventsByTypeAsync(EventType.MatchStart);
                
                Assert.That(events.Count(), Is.EqualTo(1));
                Assert.That(events.First().Item1, Is.EqualTo(EventType.MatchStart));
                Assert.That(events.First().Item2, Is.EqualTo("Match started!"));
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }
        }

        [Test]
        public async Task GetRecentEventsAsync_ReturnsCorrectCount()
        {
            try
            {
                await _eventRepository.AddEventAsync(EventType.MatchStart, "Event 1");
                await _eventRepository.AddEventAsync(EventType.KillNotification, "Event 2");
                await _eventRepository.AddEventAsync(EventType.PlayerJoined, "Event 3");

                var recentEvents = await _eventRepository.GetRecentEventsAsync(2);
                
                Assert.That(recentEvents.Count(), Is.EqualTo(2));
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }
        }
    }
} 