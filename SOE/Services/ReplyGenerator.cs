namespace SOE.Services
{
    public class ReplyGenerator
    {
        public List<string> GetHints(string mood)
        {
            return mood switch
            {
                "joy" => ["Расскажи подробнее! 😊", "Рад за тебя!"],
                "sadness" => ["Может, обсудим это? 🫂", "Я с тобой..."],
                "anger" => ["Давай остынем... ❄️", "Попробуй深呼吸"],
                _ => ["Хочешь поговорить?"]
            };
        }
    }
}
