namespace SurveyApp.Data;

public enum SurveyStatus
{
    Draft,
    Published,
    Closed,
}

public class Survey
{
    // 公開 URL に使うため、推測されにくい Guid を主キーにする
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required string OwnerId { get; set; }
    public ApplicationUser? Owner { get; set; }

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
