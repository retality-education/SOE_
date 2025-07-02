namespace SOE.Models
{
    public class ChatSummary
    {
        public string ChatId { get; set; }
        public string ChatName { get; set; }
        public DateTime LastMessageTime { get; set; }
        public string LastMessagePreview { get; set; }
    }
}
