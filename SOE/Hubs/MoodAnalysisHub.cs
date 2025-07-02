using Microsoft.AspNetCore.SignalR;
using SOE.Models.DTOs;
using SOE.Services;

namespace SOE.Hubs
{
    public class MoodAnalysisHub : Hub
    {
        private readonly EmotionService _moodService;
        private readonly ReplyGenerator _replyGenerator;

        public MoodAnalysisHub(EmotionService moodService, ReplyGenerator replyGenerator)
        {
            _moodService = moodService;
            _replyGenerator = replyGenerator;
        }
        public async Task<AnalyzedMessageDto> AnalyzeMessage(string text)
        {
            var analysis = await _moodService.AnalyzeTextAsync(text);
            var replyHints = _replyGenerator.GetHints(analysis.Mood);

            return new AnalyzedMessageDto
            {
                Mood = analysis.Mood,
                BackgroundColor = analysis.BackgroundColor,
                ReplyHints = replyHints
            };
        }
    }
}
