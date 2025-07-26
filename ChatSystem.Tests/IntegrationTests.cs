using ChatSystem.Application.DependencyInjection;
using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Enums;
using ChatSystem.Domain.Interfaces;
using ChatSystem.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ChatSystem.Tests
{
    [TestFixture]
    public class IntegrationTests
    {
        private IServiceProvider _serviceProvider;

        [SetUp]
        public void SetUp()
        {
            var services = new ServiceCollection();
            services.AddChatSystemApplication();
            services.AddChatSystemInfrastructure(50);
            _serviceProvider = services.BuildServiceProvider();
            
            var messageRepository = _serviceProvider.GetRequiredService<IChatMessageRepository>();
            messageRepository.ClearAllMessagesAsync().Wait();
        }

        [TearDown]
        public void TearDown()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        [Test]
        public async Task MultipleClients_BroadcastMessages_AllClientsReceive()
        {
            var network = _serviceProvider.GetRequiredService<IChatNetwork>();
            var factory = _serviceProvider.GetRequiredService<IChatManagerFactory>();

            var client1 = factory.CreateChatManager("Player1");
             var client2 = factory.CreateChatManager("Player2");
             var client3 = factory.CreateChatManager("Player3");

            network.AddClient("Player1");
            network.AddClient("Player2");
            network.AddClient("Player3");

            client1.Initialize();
            client2.Initialize();
            client3.Initialize();

            var client1Messages = new List<ChatMessage>();
            var client2Messages = new List<ChatMessage>();
            var client3Messages = new List<ChatMessage>();

            client1.Messages.Subscribe(msg => client1Messages.Add(msg));
            client2.Messages.Subscribe(msg => client2Messages.Add(msg));
            client3.Messages.Subscribe(msg => client3Messages.Add(msg));

            try
            {
                await client1.SendChatMessageAsync(ChatType.Public, "Hello from Player1");
                await Task.Delay(100);
                await client2.SendChatMessageAsync(ChatType.Public, "Hello from Player2");
                await Task.Delay(100);
                await client3.SendChatMessageAsync(ChatType.Public, "Hello from Player3");
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            Assert.That(client1Messages.Count, Is.EqualTo(1));
            Assert.That(client2Messages.Count, Is.EqualTo(1));
            Assert.That(client3Messages.Count, Is.EqualTo(1));

            Assert.That(client1Messages[0].Content, Is.EqualTo("Hello from Player1"));
            Assert.That(client2Messages[0].Content, Is.EqualTo("Hello from Player2"));
            Assert.That(client3Messages[0].Content, Is.EqualTo("Hello from Player3"));
        }

        [Test]
        public async Task ChatTypeFiltering_OnlyRelevantMessagesReceived()
        {
            var network = _serviceProvider.GetRequiredService<IChatNetwork>();
            var factory = _serviceProvider.GetRequiredService<IChatManagerFactory>();

            using var client1 = factory.CreateChatManager("Player1");
            using var client2 = factory.CreateChatManager("Player2");

            network.AddClient("Player1");
            network.AddClient("Player2");

            client1.Initialize();
            client2.Initialize();

            var publicMessages = new List<ChatMessage>();
            var teamMessages = new List<ChatMessage>();

            client1.GetMessagesByType(ChatType.Public).Subscribe(msg => publicMessages.Add(msg));
            client1.GetMessagesByType(ChatType.Team).Subscribe(msg => teamMessages.Add(msg));

            try
            {
                await client2.SendChatMessageAsync(ChatType.Public, "Public message");
                await client2.SendChatMessageAsync(ChatType.Team, "Team message");
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            Assert.That(publicMessages.Count, Is.EqualTo(1));
            Assert.That(teamMessages.Count, Is.EqualTo(1));
            Assert.That(publicMessages[0].Content, Is.EqualTo("Public message"));
            Assert.That(teamMessages[0].Content, Is.EqualTo("Team message"));
        }

        [Test]
        public async Task Authority_DuplicateMessagesFiltered()
        {
            var network = _serviceProvider.GetRequiredService<IChatNetwork>();
            var factory = _serviceProvider.GetRequiredService<IChatManagerFactory>();

            using var client1 = factory.CreateChatManager("Player1");
            using var client2 = factory.CreateChatManager("Player2");

            network.AddClient("Player1");
            network.AddClient("Player2");

            client1.Initialize();
            client2.Initialize();

            var receivedMessages = new List<ChatMessage>();
            client2.Messages.Subscribe(msg => receivedMessages.Add(msg));

            try
            {
                await client1.SendChatMessageAsync(ChatType.Public, "Duplicate message");
                await client1.SendChatMessageAsync(ChatType.Public, "Duplicate message");
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            Assert.That(receivedMessages.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task DisconnectReconnect_RetryMechanismWorks()
        {
            var network = _serviceProvider.GetRequiredService<IChatNetwork>();
            var factory = _serviceProvider.GetRequiredService<IChatManagerFactory>();

            using var client1 = factory.CreateChatManager("Player1");
            network.AddClient("Player1");

            client1.Initialize();

            var receivedMessages = new List<ChatMessage>();
            client1.Messages.Subscribe(msg => receivedMessages.Add(msg));

            network.SimulateDisconnect();
            try
            {
                await client1.SendChatMessageAsync(ChatType.Public, "Message during disconnect");
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            network.SimulateReconnect();
            try
            {
                await client1.SendChatMessageAsync(ChatType.Public, "Message after reconnect");
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            Assert.That(receivedMessages.Count, Is.EqualTo(1));
            Assert.That(receivedMessages[0].Content, Is.EqualTo("Message after reconnect"));
        }

        [Test]
        public async Task Notifications_BroadcastToAllClients()
        {
            var network = _serviceProvider.GetRequiredService<IChatNetwork>();
            var factory = _serviceProvider.GetRequiredService<IChatManagerFactory>();

            using var client1 = factory.CreateChatManager("Player1");
            using var client2 = factory.CreateChatManager("Player2");

            network.AddClient("Player1");
            network.AddClient("Player2");

            client1.Initialize();
            client2.Initialize();

            var client1Events = new List<(EventType, object)>();
            var client2Events = new List<(EventType, object)>();

            client1.Events.Subscribe(ev => client1Events.Add(ev));
            client2.Events.Subscribe(ev => client2Events.Add(ev));

            try
            {
                await client1.SendNotificationAsync(EventType.MatchStart, "Match started!");
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            Assert.That(client1Events.Count, Is.EqualTo(1));
            Assert.That(client2Events.Count, Is.EqualTo(1));
            Assert.That(client1Events[0].Item1, Is.EqualTo(EventType.MatchStart));
            Assert.That(client2Events[0].Item1, Is.EqualTo(EventType.MatchStart));
        }

        [Test]
        public void ServiceRegistration_AllServicesResolved()
        {
            Assert.That(() => _serviceProvider.GetRequiredService<IChatNetwork>(), Throws.Nothing);
            Assert.That(() => _serviceProvider.GetRequiredService<IChatMediator>(), Throws.Nothing);
            Assert.That(() => _serviceProvider.GetRequiredService<IChatManagerFactory>(), Throws.Nothing);
        }

        [Test]
        public void ChatManagerFactory_CreatesManagersWithCorrectClientId()
        {
            var factory = _serviceProvider.GetRequiredService<IChatManagerFactory>();

            using var client1 = factory.CreateChatManager("Player1");
            using var client2 = factory.CreateChatManager("Player2");

            Assert.That(client1.ClientId, Is.EqualTo("Player1"));
            Assert.That(client2.ClientId, Is.EqualTo("Player2"));
        }
    }
} 