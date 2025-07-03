using SOE.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SOE.Services
{


    public class EmotionService
    {
        private readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("http://emotion_service:5001")
        };

        public async Task<(string Emotion, float Confidence)> AnalyzeEmotion(string text)
        {
            var response = await _httpClient.PostAsJsonAsync("/analyze", new { text });
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EmotionResult>();
            return (result.Emotion, result.Confidence);
        }

        private record EmotionResult(
            [property: JsonPropertyName("emotion")] string Emotion,
            [property: JsonPropertyName("confidence")] float Confidence
        );

        public async Task<MoodAnalysisResult> AnalyzeTextAsync(string text)
        {
            var (emotion, confidence) = await AnalyzeEmotion(text);

            return new MoodAnalysisResult
            {
                Mood = DetectMood(emotion, confidence),
                Hint = GetHint(emotion),
                BackgroundColor = GetColor(emotion)
            };
        }

        private string DetectMood(string emotion, float confidence)
        {
            if (confidence < 0.5f)
                return "Neutral";

            return emotion switch
            {
                "joy" => "Happy",
                "anger" => "Angry",
                "sadness" => "Sad",
                "fear" => "Afraid",
                "love" => "Loved",
                "surprise" => "Surprised",
                _ => "Neutral"
            };
        }

        private string GetHint(string emotion)
        {
            return emotion switch
            {
                "joy" => "Keep smiling!",
                "anger" => "Take a deep breath.",
                "sadness" => "It's okay to feel sad sometimes.",
                "fear" => "Stay strong and face your fears.",
                "love" => "Love is in the air!",
                "surprise" => "Expect the unexpected!",
                _ => ""
            };
        }

        private string GetColor(string emotion)
        {
            return emotion switch
            {
                "joy" => "#FFD700",      // золотой
                "anger" => "#FF4500",    // оранжево-красный
                "sadness" => "#1E90FF",  // синий
                "fear" => "#800080",     // фиолетовый
                "love" => "#FF69B4",     // розовый
                "surprise" => "#00FFFF", // бирюзовый
                _ => "#808080"           // серый по умолчанию
            };
        }
    }

    public class MoodAnalysisResult
    {
        public string Mood { get; init; }
        public string Hint { get; init; }
        public string BackgroundColor { get; init; }
    }

}
