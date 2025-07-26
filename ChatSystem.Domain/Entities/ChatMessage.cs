using System;
using ChatSystem.Domain.Enums;

namespace ChatSystem.Domain.Entities
{
    public class ChatMessage
    {
        public ChatType Type { get; }
        public string Sender { get; }
        public string Content { get; }

        public ChatMessage(ChatType type, string sender, string content)
        {
            Type = type;
            Sender = sender;
            Content = content;
        }

        public override string ToString()
        {
            return $"[{Type}] {Sender}: {Content}";
        }
    }
} 