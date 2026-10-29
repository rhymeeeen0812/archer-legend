namespace SurveyApp.Data;

public enum SurveyStatus
{
    Draft,
    Published,
    Closed,
}

public class Survey
{
    // ゲームから参照する ID を推測されにくくするため Guid にする
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required string Title { get; set; }
    public string? Description { get; set; }
    public SurveyStatus Status { get; set; } = SurveyStatus.Draft;

    // null のときは期限なし
    public DateTimeOffset? ClosesAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<Question> Questions { get; set; } = [];
    public List<Response> Responses { get; set; } = [];

    public bool CanAcceptResponses(DateTimeOffset now) =>
        Status == SurveyStatus.Published && (ClosesAt is null || now < ClosesAt);
}
