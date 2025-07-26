using ChatSystem.Application.Services;
using ChatSystem.Application.DependencyInjection;
using ChatSystem.Domain.Entities;
using ChatSystem.Domain.Enums;
using ChatSystem.Domain.Interfaces;
using ChatSystem.Infrastructure.Network;
using ChatSystem.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace ChatSystem.Demo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Chat System Demo ===");
            Console.WriteLine("Демонстрация мультиклиентской чат-системы с фильтрацией и authority\n");

            CancellationTokenSource cancellationToken = new CancellationTokenSource();
            (IChatNetwork network, IChatManager client1, IChatManager client2, IChatManager client3) = SetupChatSystem();

            await RunDemoAsync(network, client1, client2, client3, cancellationToken.Token);

            CleanupAsync(client1, client2, client3,cancellationToken);

            Console.WriteLine("=== Демо завершено ===");
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        private static (IChatNetwork, IChatManager, IChatManager, IChatManager) SetupChatSystem()
        {
            var services = new ServiceCollection();
            services.AddChatSystemInfrastructure(150);
            services.AddChatSystemApplication();
            var serviceProvider = services.BuildServiceProvider();

            var network = serviceProvider.GetRequiredService<IChatNetwork>();
            var factory = serviceProvider.GetRequiredService<IChatManagerFactory>();

            var client1 = factory.CreateChatManager("Player1");
            var client2 = factory.CreateChatManager("Player2");
            var client3 = factory.CreateChatManager("Player3");

            network.AddClient("Player1");
            network.AddClient("Player2");
            network.AddClient("Player3");

            client1.Initialize();
            client2.Initialize();
            client3.Initialize();

            return (network, client1, client2, client3);
        }

        private static async Task RunDemoAsync(IChatNetwork network, IChatManager client1, IChatManager client2,
            IChatManager client3, CancellationToken cancellationToken)
        {
            SetupMessageHandlers(client1, client2, client3);
            SetupEventHandlers(client1, client2, client3);

            Console.WriteLine("Отправляем сообщения...\n");

            await DemonstratePublicChatAsync(client1, client2, client3, cancellationToken);
            await DemonstrateTeamChatAsync(client1, client2, client3, cancellationToken);
            await DemonstrateNotificationsAsync(client1, client2, client3, cancellationToken);
            await DemonstrateAuthorityAsync(client1, cancellationToken);
            await DemonstrateDisconnectReconnectAsync(client1, network, cancellationToken);
            await DemonstrateTypeFilteringAsync(client1, client2, cancellationToken);
        }

        private static void SetupMessageHandlers(IChatManager client1, IChatManager client2, IChatManager client3)
        {
            client1.Messages.Subscribe(msg => Console.WriteLine($"Client1 received: {msg}"));
            client2.Messages.Subscribe(msg => Console.WriteLine($"Client2 received: {msg}"));
            client3.Messages.Subscribe(msg => Console.WriteLine($"Client3 received: {msg}"));
        }

        private static void SetupEventHandlers(IChatManager client1, IChatManager client2, IChatManager client3)
        {
            client1.Events.Subscribe(ev => Console.WriteLine($"Client1 event: {ev.Item1} - {ev.Item2}"));
            client2.Events.Subscribe(ev => Console.WriteLine($"Client2 event: {ev.Item1} - {ev.Item2}"));
            client3.Events.Subscribe(ev => Console.WriteLine($"Client3 event: {ev.Item1} - {ev.Item2}"));
        }

        private static async Task DemonstratePublicChatAsync(IChatManager client1, IChatManager client2, IChatManager client3,
            CancellationToken cancellationToken)
        {
            Console.WriteLine("=== Публичный чат ===");

            try
            {
                await client1.SendChatMessageAsync(ChatType.Public, "Привет всем!", cancellationToken);
                await Task.Delay(100, cancellationToken);
                await client2.SendChatMessageAsync(ChatType.Public, "Привет! Как дела?", cancellationToken);
                await Task.Delay(100, cancellationToken);
                await client3.SendChatMessageAsync(ChatType.Public, "Всем привет!", cancellationToken);
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in public chat: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task DemonstrateTeamChatAsync(IChatManager client1, IChatManager client2, IChatManager client3, CancellationToken cancellationToken)
        {
            Console.WriteLine("=== Командный чат ===");

            try
            {
                await client1.SendChatMessageAsync(ChatType.Team, "Командная стратегия: атакуем базу!",
                    cancellationToken);
                await Task.Delay(100, cancellationToken);
                await client2.SendChatMessageAsync(ChatType.Team, "Понял, иду на базу", cancellationToken);
                await Task.Delay(100, cancellationToken);
                await client3.SendChatMessageAsync(ChatType.Team, "Поддерживаю!", cancellationToken);
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in team chat: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task DemonstrateNotificationsAsync(IChatManager client1, IChatManager client2,
            IChatManager client3, CancellationToken cancellationToken)
        {
            Console.WriteLine("=== Уведомления ===");

            try
            {
                await client1.SendNotificationAsync(EventType.MatchStart, "Матч начался!", cancellationToken);
                await Task.Delay(100, cancellationToken);
                await client2.SendNotificationAsync(EventType.KillNotification, "Player2 убил Player3", cancellationToken);
                await Task.Delay(100, cancellationToken);
                await client3.SendNotificationAsync(EventType.KillNotification, "Player3 убил Player2", cancellationToken);
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in notifications: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task DemonstrateAuthorityAsync(IChatManager client1, CancellationToken cancellationToken)
        {
            Console.WriteLine("=== Authority (фильтрация дубликатов) ===");

            try
            {
                await client1.SendChatMessageAsync(ChatType.Public, "Тестовое сообщение", cancellationToken);
                await Task.Delay(100, cancellationToken);
                await client1.SendChatMessageAsync(ChatType.Public, "Тестовое сообщение", cancellationToken);
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in authority test: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task DemonstrateDisconnectReconnectAsync(IChatManager client1, IChatNetwork network,
            CancellationToken cancellationToken)
        {
            Console.WriteLine("=== Disconnect/Reconnect ===");

            try
            {
                network.SimulateDisconnect();
                await client1.SendChatMessageAsync(ChatType.Public, "Сообщение при отключении", cancellationToken);
                await Task.Delay(100, cancellationToken);

                network.SimulateReconnect();
                await client1.SendChatMessageAsync(ChatType.Public, "Сообщение после переподключения",
                    cancellationToken);
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in disconnect/reconnect: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task DemonstrateTypeFilteringAsync(IChatManager client1, IChatManager client2,
            CancellationToken cancellationToken)
        {
            Console.WriteLine("=== Фильтрация по типу ===");

            var publicMessages = new List<ChatMessage>();
            var teamMessages = new List<ChatMessage>();

            client1.GetMessagesByType(ChatType.Public).Subscribe(msg =>
            {
                publicMessages.Add(msg);
                Console.WriteLine($"Public only: {msg}");
            });

            client1.GetMessagesByType(ChatType.Team).Subscribe(msg =>
            {
                teamMessages.Add(msg);
                Console.WriteLine($"Team only: {msg}");
            });

            try
            {
                await client2.SendChatMessageAsync(ChatType.Public, "Публичное сообщение для фильтрации",
                    cancellationToken);
                await Task.Delay(100, cancellationToken);
                await client2.SendChatMessageAsync(ChatType.Team, "Командное сообщение для фильтрации",
                    cancellationToken);
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in type filtering: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static void CleanupAsync(IChatManager client1, IChatManager client2, IChatManager client3,
            CancellationTokenSource cancellationToken)
        {
            try
            {
                cancellationToken?.Cancel();
                cancellationToken?.Dispose();
                
                client1.Dispose();
                client2.Dispose();
                client3.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }
    }
}