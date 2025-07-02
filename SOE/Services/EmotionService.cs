using SOE.Models;

namespace SOE.Services
{
    public class EmotionService
    {
        private readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("http://localhost:5001")
        };

        public async Task<(string Emotion, float Confidence)> AnalyzeEmotion(string text)
        {
            var response = await _httpClient.PostAsJsonAsync("/analyze", new { text });
            var result = await response.Content.ReadFromJsonAsync<EmotionResult>();
            return (result.Emotion, result.Confidence);
        }

        private record EmotionResult(string Emotion, float Confidence);
        public async Task<MoodAnalysisResult> AnalyzeTextAsync(string text)
        {
            // Реализация через ML.NET или OpenAI API
            return new MoodAnalysisResult
            {
                Mood = DetectMood(text), // "joy", "anger", "sadness"
                Hint = GetHint(text),
                BackgroundColor = GetColor(text)
            };
        }

        private string DetectMood(string text) => throw new Exception();
        private string GetHint(string text) => throw new Exception();
        private string GetColor(string text) => throw new Exception();
    }
}
