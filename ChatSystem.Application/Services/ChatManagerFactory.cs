using ChatSystem.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ChatSystem.Application.Services
{
    public class ChatManagerFactory : IChatManagerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ChatManagerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IChatManager CreateChatManager(string clientId)
        {
            var mediator = _serviceProvider.GetRequiredService<IChatMediator>();
            return new ChatManager(mediator, clientId);
        }
    }
} 