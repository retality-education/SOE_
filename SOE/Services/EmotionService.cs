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
            var mood = DetectMood(emotion, confidence);
            var phrases = GetPhrases(emotion);

            return new MoodAnalysisResult
            {
                Mood = mood,
                Hint = GetHint(emotion),
                BackgroundColor = GetColor(emotion),
                TextColor = GetTextColor(emotion),
                SuggestedPhrases = phrases.OrderBy(_ => Guid.NewGuid()).Take(3).ToArray()
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
        private string GetTextColor(string emotion)
        {
            return emotion switch
            {
                "joy" => "#000000",         // чёрный на жёлтом
                "anger" => "#FFFFFF",       // белый на красном
                "sadness" => "#FFFFFF",     // белый на синем
                "fear" => "#FFFFFF",        // белый на фиолетовом
                "love" => "#000000",        // чёрный на розовом
                "surprise" => "#000000",    // чёрный на бирюзовом
                _ => "#000000"
            };
        }

        private string[] GetPhrases(string emotion)
        {
            return emotion switch
            {
                "joy" => new[]
                {
            "Рад тебя видеть!", "Какой прекрасный день!", "Делись хорошим настроением!", "Ты излучаешь свет!", "Продолжай в том же духе!"
        },
                "anger" => new[]
                {
            "Попробуй сделать вдох-выдох.", "Может сделать паузу?", "Ты можешь справиться с этим.", "Не давай гневу управлять собой.", "Поговорим об этом спокойно?"
        },
                "sadness" => new[]
                {
            "Я рядом.", "Ты не один.", "Все наладится.", "Может поговорим?", "Хочешь обнять?"
        },
                "fear" => new[]
                {
            "Ты в безопасности.", "Я с тобой.", "Ты сильнее, чем думаешь.", "Не бойся говорить о страхе.", "Ты сможешь это преодолеть."
        },
                "love" => new[]
                {
            "Это звучит мило!", "Любовь витает в воздухе!", "Улыбка тебе к лицу!", "Ты делаешь этот мир лучше.", "Расскажи больше!"
        },
                "surprise" => new[]
                {
            "Вот это поворот!", "Неожиданно!", "Расскажи подробнее!", "Интересно, что будет дальше?", "Ничего себе!"
        },
                _ => Array.Empty<string>()
            };
        }
    }

}
