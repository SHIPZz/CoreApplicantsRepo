using ChatSystem.Application.Services;
using ChatSystem.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ChatSystem.Application.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddChatSystemApplication(this IServiceCollection services)
        {
            services.AddSingleton<IChatMediator, ChatMediator>();
            services.AddSingleton<IChatManagerFactory, ChatManagerFactory>();
            
            return services;
        }
    }
} 