namespace SOE.Models
{
    public class Message
    {
        public Guid Id { get; set; }
        public string ChatId { get; set; }  // ID чата (или диалога)
        public string UserId { get; set; }  // ID отправителя
        public string Text { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
