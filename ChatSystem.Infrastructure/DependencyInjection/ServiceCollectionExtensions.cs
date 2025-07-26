using ChatSystem.Domain.Interfaces;
using ChatSystem.Infrastructure.Network;
using ChatSystem.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ChatSystem.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddChatSystemInfrastructure(this IServiceCollection services, int networkLatencyMs = 100)
        {
            services.AddSingleton<IChatNetwork>(_ => new MockChatNetwork(networkLatencyMs));
            services.AddSingleton<IChatMessageRepository, ChatMessageRepository>();
            services.AddSingleton<IEventRepository, EventRepository>();
            
            return services;
        }
    }
} 