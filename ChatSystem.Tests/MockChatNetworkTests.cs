using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Enums;
using ChatSystem.Infrastructure.Network;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ChatSystem.Tests
{
    [TestFixture]
    public class MockChatNetworkTests
    {
        private MockChatNetwork _network;
        private List<ChatMessage> _receivedMessages;
        private List<(EventType, object)> _receivedEvents;

        [SetUp]
        public void SetUp()
        {
            _network = new MockChatNetwork(50);
            _receivedMessages = new List<ChatMessage>();
            _receivedEvents = new List<(EventType, object)>();

            _network.OnMessageReceived.Subscribe(msg => _receivedMessages.Add(msg));
            _network.OnEventReceived.Subscribe(ev => _receivedEvents.Add(ev));
        }

        [TearDown]
        public void TearDown()
        {
            _network = null;
            _receivedMessages?.Clear();
            _receivedEvents?.Clear();
        }

        [Test]
        public async Task SendMessageAsync_BroadcastsToAllClients()
        {
            _network.AddClient("Player1");
            _network.AddClient("Player2");

            var message = new ChatMessage(ChatType.Public, "Player1", "Hello");

            try
            {
                await _network.SendMessageAsync(message);
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            Assert.That(_receivedMessages.Count, Is.EqualTo(1));
            Assert.That(_receivedMessages[0], Is.EqualTo(message));
        }

        [Test]
        public async Task RaiseEventAsync_BroadcastsToAllClients()
        {
            var testNetwork = new MockChatNetwork(50);
            var testEvents = new List<(EventType, object)>();
            testNetwork.OnEventReceived.Subscribe(ev => testEvents.Add(ev));

            testNetwork.AddClient("Player1");
            testNetwork.AddClient("Player2");

            try
            {
                await testNetwork.RaiseEventAsync(EventType.MatchStart, "Match started!");
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            Assert.That(testEvents.Count, Is.EqualTo(3));
            Assert.That(testEvents[0].Item1, Is.EqualTo(EventType.PlayerJoined));
            Assert.That(testEvents[1].Item1, Is.EqualTo(EventType.PlayerJoined));
            Assert.That(testEvents[2].Item1, Is.EqualTo(EventType.MatchStart));
            Assert.That(testEvents[2].Item2, Is.EqualTo("Match started!"));
        }

        [Test]
        public void AddClient_NotifiesPlayerJoined()
        {
            try
            {
                _network.AddClient("Player1");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            Assert.That(_receivedEvents.Count, Is.EqualTo(1));
            Assert.That(_receivedEvents[0].Item1, Is.EqualTo(EventType.PlayerJoined));
            Assert.That(_receivedEvents[0].Item2, Is.EqualTo("Player1"));
        }

        [Test]
        public void RemoveClient_NotifiesPlayerLeft()
        {
            _network.AddClient("Player1");

            try
            {
                _network.RemoveClient("Player1");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            Assert.That(_receivedEvents.Count, Is.EqualTo(2));
            Assert.That(_receivedEvents[1].Item1, Is.EqualTo(EventType.PlayerLeft));
            Assert.That(_receivedEvents[1].Item2, Is.EqualTo("Player1"));
        }

        [Test]
        public async Task SimulateDisconnect_ThrowsException()
        {
            _network.SimulateDisconnect();

            var message = new ChatMessage(ChatType.Public, "Player1", "Test");

            try
            {
                await _network.SendMessageAsync(message);
                Assert.Fail("Expected exception was not thrown");
            }
            catch (InvalidOperationException ex)
            {
                Assert.That(ex.Message, Is.EqualTo("Disconnected"));
            }
        }

        [Test]
        public async Task SimulateReconnect_AllowsMessagesAgain()
        {
            _network.SimulateDisconnect();
            _network.SimulateReconnect();

            var message = new ChatMessage(ChatType.Public, "Player1", "Test");

            try
            {
                await _network.SendMessageAsync(message);
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            Assert.That(_receivedMessages.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task DuplicateMessage_IsFiltered()
        {
            var message = new ChatMessage(ChatType.Public, "Player1", "Duplicate message");

            try
            {
                await _network.SendMessageAsync(message);
                await _network.SendMessageAsync(message);
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            Assert.That(_receivedMessages.Count, Is.EqualTo(1));
        }
    }
} 