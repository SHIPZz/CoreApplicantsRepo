namespace ChatSystem.Domain.Interfaces
{
    public interface IChatManagerFactory
    {
        IChatManager CreateChatManager(string clientId);
    }
} 