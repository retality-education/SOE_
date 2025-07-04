namespace SOE.Models
{
    public class Message
    {
        public Guid Id { get; set; }
        public string ChatId { get; set; }
        public string UserId { get; set; }
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
