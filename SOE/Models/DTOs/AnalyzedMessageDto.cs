namespace SOE.Models.DTOs
{
    public class AnalyzedMessageDto
    {
        public string Mood { get; set; }
        public string BackgroundColor { get; set; }
        public List<string> ReplyHints { get; set; } = new();
    }
}
