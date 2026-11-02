using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SurveyApp.Data;

namespace SurveyApp.Api;

// ゲームから呼ばれるアンケートの API（回答できるアンケートの取得と回答の送信）
public static class SurveyGameApi
{
    public static IEndpointRouteBuilder MapSurveyGameApi(this IEndpointRouteBuilder app)
    {
        var surveys = app.MapGroup("/api/players/{playerId:guid}/surveys").WithTags("Surveys").AllowAnonymous();
        surveys.MapGet("/", GetAvailableSurveys);
        surveys.MapPost("/{surveyId:guid}/responses", SubmitResponse);
        return app;
    }

    // 公開中・締め切り前で、このプレイヤーがまだ回答していないアンケート（新しく公開された順）
    private static async Task<Results<Ok<GameSurveyListResponse>, NotFound>> GetAvailableSurveys(
        Guid playerId, ApplicationDbContext db, TimeProvider time)
    {
        if (!await db.Players.AnyAsync(p => p.Id == playerId))
        {
            return TypedResults.NotFound();
        }

        var now = time.GetUtcNow();
        var surveys = await db.Surveys
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Questions).ThenInclude(q => q.Choices)
            .Where(s => s.Status == SurveyStatus.Published && (s.ClosesAt == null || s.ClosesAt > now))
            .Where(s => !s.Responses.Any(r => r.PlayerId == playerId))
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();

        return TypedResults.Ok(new GameSurveyListResponse(surveys.Select(ToResponse).ToList()));
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> SubmitResponse(
        Guid playerId, Guid surveyId, SubmitSurveyResponseRequest request, ApplicationDbContext db, TimeProvider time)
    {
        if (!await db.Players.AnyAsync(p => p.Id == playerId))
        {
            return TypedResults.NotFound();
        }

        var survey = await db.Surveys
            .AsSplitQuery()
            .Include(s => s.Questions).ThenInclude(q => q.Choices)
            .FirstOrDefaultAsync(s => s.Id == surveyId && s.Status != SurveyStatus.Draft);
        if (survey is null)
        {
            return TypedResults.NotFound();
        }

        var now = time.GetUtcNow();
        if (!survey.CanAcceptResponses(now))
        {
            return TypedResults.Problem("このアンケートは受付を終了しました。", statusCode: StatusCodes.Status409Conflict);
        }

        if (await db.Responses.AnyAsync(r => r.SurveyId == surveyId && r.PlayerId == playerId))
        {
            return AlreadyAnswered();
        }

        if (request.Answers?.Any(a => a is null) == true)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["answers"] = ["回答に空の要素が含まれています。"],
            });
        }

        var submitted = request.Answers?.Select(a => new SubmittedAnswer(a.QuestionId, a.ChoiceIds, a.Text)).ToList() ?? [];
        var (answers, errors) = survey.BuildAnswers(submitted);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        db.Responses.Add(new Response { SurveyId = surveyId, PlayerId = playerId, SubmittedAt = now, Answers = answers });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // 同じ回答が同時に届いた（送信ボタンの連打など）
            return AlreadyAnswered();
        }

        return TypedResults.NoContent();
    }

    private static ProblemHttpResult AlreadyAnswered() =>
        TypedResults.Problem("このアンケートには回答済みです。", statusCode: StatusCodes.Status409Conflict);

    private static GameSurveyResponse ToResponse(Survey survey) => new(
        survey.Id,
        survey.Title,
        survey.Description,
        survey.Questions.OrderBy(q => q.Order).Select(q => new GameQuestionResponse(
            q.Id,
            q.Text,
            q.Type.ToString(),
            q.IsRequired,
            q.Choices.OrderBy(c => c.Order).Select(c => new GameChoiceResponse(c.Id, c.Text)).ToList())).ToList());
}
