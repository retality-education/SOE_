namespace SOE.Models
{
    public class MoodAnalysisResult
    {
        public string Mood { get; set; }
        public string Hint { get; set; }       // Подсказка для отправителя
        public string BackgroundColor { get; set; }  // HEX-цвет фона
    }
}
