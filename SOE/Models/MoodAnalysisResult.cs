namespace SOE.Models
{
    public class MoodAnalysisResult
    {
        public string Mood { get; init; }
        public string Hint { get; init; }
        public string BackgroundColor { get; init; }
        public string TextColor { get; init; }
        public string[] SuggestedPhrases { get; init; }
    }

}
