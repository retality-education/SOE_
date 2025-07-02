namespace SOE.Models
{
    public class ChatMessage
    {
        public Guid Id { get; set; }          // ID сообщения
        public string ChatId { get; set; }    // ID чата
        public string UserId { get; set; }
        public string Text { get; set; }      // Текст сообщения
        public DateTime Timestamp { get; set; } // Время отправки
    }
}
