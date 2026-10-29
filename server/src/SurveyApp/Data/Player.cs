namespace SurveyApp.Data;

// ゲームのプレイヤー。ログインは行わず、ゲームが初回起動時に作った Id で識別する。
public class Player
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
