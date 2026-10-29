using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SurveyApp.Data;

namespace SurveyApp.Api;

// ゲームから呼ばれる API（プレイヤー登録・戦績・ランキング）。
// ログインは行わず、ゲームが初回起動時に生成したプレイヤー Id で識別する。
public static class GameApi
{
    public const int DefaultRankingLimit = 10;
    public const int MaxRankingLimit = 100;

    public static IEndpointRouteBuilder MapGameApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        var players = api.MapGroup("/players").WithTags("Players");
        players.MapPost("/", RegisterPlayer);
        players.MapGet("/{id:guid}", GetPlayer);
        players.MapPut("/{id:guid}/name", RenamePlayer);
        players.MapPost("/{id:guid}/runs", SubmitRun);

        api.MapGet("/rankings", GetRanking).WithTags("Rankings");
        return app;
    }

    // 同じ Id で再送されても 1 人として扱う（初回起動時の通信が途中で切れても登録し直せるように）
    private static async Task<Results<Created<PlayerResponse>, Ok<PlayerResponse>, ValidationProblem>> RegisterPlayer(
        RegisterPlayerRequest request, ApplicationDbContext db, TimeProvider time)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.Id == Guid.Empty)
        {
            errors["id"] = ["プレイヤー Id を指定してください。"];
        }

        if (!Player.TryNormalizeName(request.Name, out var name))
        {
            errors["name"] = [$"名前は 1〜{Player.NameMaxLength} 文字で入力してください。"];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var existing = await db.Players.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.Id);
        if (existing is not null)
        {
            return TypedResults.Ok(ToResponse(existing));
        }

        var player = new Player { Id = request.Id, Name = name, CreatedAt = time.GetUtcNow() };
        db.Players.Add(player);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // 同じ Id の登録が同時に届いた。先に登録された方を返す
            db.ChangeTracker.Clear();
            var registered = await db.Players.AsNoTracking().FirstAsync(p => p.Id == request.Id);
            return TypedResults.Ok(ToResponse(registered));
        }

        return TypedResults.Created($"/api/players/{player.Id}", ToResponse(player));
    }

    private static async Task<Results<Ok<PlayerResponse>, NotFound>> GetPlayer(Guid id, ApplicationDbContext db)
    {
        var player = await db.Players.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return player is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(player));
    }

    private static async Task<Results<Ok<PlayerResponse>, NotFound, ValidationProblem>> RenamePlayer(
        Guid id, RenamePlayerRequest request, ApplicationDbContext db)
    {
        if (!Player.TryNormalizeName(request.Name, out var name))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = [$"名前は 1〜{Player.NameMaxLength} 文字で入力してください。"],
            });
        }

        var player = await db.Players.FirstOrDefaultAsync(p => p.Id == id);
        if (player is null)
        {
            return TypedResults.NotFound();
        }

        player.Name = name;
        await db.SaveChangesAsync();
        return TypedResults.Ok(ToResponse(player));
    }

    // 1 回分のプレイ結果を記録して戦績を更新する。RunId が同じ再送は二重に数えず、最初の結果を返す
    private static async Task<Results<Ok<SubmitRunResponse>, NotFound, ValidationProblem, ProblemHttpResult>> SubmitRun(
        Guid id, SubmitRunRequest request, ApplicationDbContext db, TimeProvider time)
    {
        var errors = ValidateRun(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var player = await db.Players.FirstOrDefaultAsync(p => p.Id == id);
        if (player is null)
        {
            return TypedResults.NotFound();
        }

        var recorded = await db.Runs.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RunId);
        if (recorded is not null)
        {
            return recorded.PlayerId == id
                ? TypedResults.Ok(new SubmitRunResponse(ToRecords(player), recorded.IsNewBestScore, recorded.IsNewBestWave))
                : TypedResults.Problem("この RunId は別のプレイヤーの結果として記録されています。", statusCode: StatusCodes.Status409Conflict);
        }

        var now = time.GetUtcNow();
        var (isNewBestScore, isNewBestWave) = player.RecordRun(request.Score, request.Wave, request.EnemiesDefeated, now);
        db.Runs.Add(new Run
        {
            Id = request.RunId,
            PlayerId = id,
            Score = request.Score,
            Wave = request.Wave,
            EnemiesDefeated = request.EnemiesDefeated,
            Level = request.Level,
            IsNewBestScore = isNewBestScore,
            IsNewBestWave = isNewBestWave,
            PlayedAt = now,
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException || (ex is DbUpdateException && IsUniqueViolation(ex)))
        {
            // 同じプレイヤーの結果が同時に届いた。ゲーム側で同じ RunId のまま送り直してもらう
            return TypedResults.Problem("同時に別の結果が記録されました。もう一度送信してください。", statusCode: StatusCodes.Status409Conflict);
        }

        return TypedResults.Ok(new SubmitRunResponse(ToRecords(player), isNewBestScore, isNewBestWave));
    }

    // スコアの高い順、同点なら先に達成した順。playerId を渡すと、上位に入っていなくても自分の順位を返す
    private static async Task<Results<Ok<RankingResponse>, ValidationProblem>> GetRanking(
        ApplicationDbContext db, Guid? playerId, int? limit)
    {
        var take = limit ?? DefaultRankingLimit;
        if (take is < 1 or > MaxRankingLimit)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["limit"] = [$"limit は 1〜{MaxRankingLimit} で指定してください。"],
            });
        }

        // 一度もスコアを出していないプレイヤーはランキングに載せない
        var ranked = db.Players.AsNoTracking().Where(p => p.BestScore > 0);

        var top = await ranked
            .OrderByDescending(p => p.BestScore)
            .ThenBy(p => p.BestScoreAchievedAt)
            .ThenBy(p => p.Id)
            .Take(take)
            .Select(p => new { p.Id, p.Name, p.BestScore, p.BestWave })
            .ToListAsync();

        var entries = top
            .Select((p, index) => new RankingEntryResponse(index + 1, p.Name, p.BestScore, p.BestWave, p.Id == playerId))
            .ToList();

        RankingEntryResponse? me = null;
        if (playerId is not null)
        {
            var player = await ranked.FirstOrDefaultAsync(p => p.Id == playerId);
            if (player is not null)
            {
                var betterCount = await ranked.CountAsync(p =>
                    p.BestScore > player.BestScore ||
                    (p.BestScore == player.BestScore && p.BestScoreAchievedAt < player.BestScoreAchievedAt));
                me = new RankingEntryResponse(betterCount + 1, player.Name, player.BestScore, player.BestWave, true);
            }
        }

        return TypedResults.Ok(new RankingResponse(entries, me));
    }

    private static Dictionary<string, string[]> ValidateRun(SubmitRunRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.RunId == Guid.Empty)
        {
            errors["runId"] = ["RunId を指定してください。"];
        }

        if (request.Score < 0)
        {
            errors["score"] = ["score は 0 以上で指定してください。"];
        }

        if (request.Wave < 1)
        {
            errors["wave"] = ["wave は 1 以上で指定してください。"];
        }

        if (request.EnemiesDefeated < 0)
        {
            errors["enemiesDefeated"] = ["enemiesDefeated は 0 以上で指定してください。"];
        }

        if (request.Level < 1)
        {
            errors["level"] = ["level は 1 以上で指定してください。"];
        }

        return errors;
    }

    private static bool IsUniqueViolation(Exception ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static PlayerResponse ToResponse(Player player) => new(player.Id, player.Name, ToRecords(player));

    private static PlayerRecordsResponse ToRecords(Player player) =>
        new(player.BestScore, player.BestWave, player.TotalScore, player.TotalEnemiesDefeated, player.PlayCount);
}
