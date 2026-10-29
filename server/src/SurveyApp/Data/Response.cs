namespace SurveyApp.Data;

// 1 回分の回答（回答者がフォームを送信した単位）
public class Response
{
    public int Id { get; set; }

    public Guid SurveyId { get; set; }
    public Survey? Survey { get; set; }

    public Guid PlayerId { get; set; }
    public Player? Player { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }

    public List<Answer> Answers { get; set; } = [];
}

// 選択式は「選んだ選択肢 1 つにつき 1 行」、自由記述は「1 問につき 1 行」で保存する。
// こうすると選択肢ごとの集計が Answers を ChoiceId で GROUP BY するだけで済む。
public class Answer
{
    public int Id { get; set; }

    public int ResponseId { get; set; }
    public Response? Response { get; set; }

    public int QuestionId { get; set; }
    public Question? Question { get; set; }

    // 選択式のときに設定
    public int? ChoiceId { get; set; }
    public Choice? Choice { get; set; }

    // 自由記述のときに設定
    public string? Text { get; set; }
}
