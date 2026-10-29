namespace SurveyApp.Api;

// ゲームとの間でやり取りする JSON の形。DB のエンティティとは分けて、公開する項目を絞る。

public record RegisterPlayerRequest(Guid Id, string? Name);

public record RenamePlayerRequest(string? Name);

public record PlayerResponse(Guid Id, string Name, PlayerRecordsResponse Records);

public record PlayerRecordsResponse(int BestScore, int BestWave, long TotalScore, int TotalEnemiesDefeated, int PlayCount);

public record SubmitRunRequest(Guid RunId, int Score, int Wave, int EnemiesDefeated, int Level);

public record SubmitRunResponse(PlayerRecordsResponse Records, bool IsNewBestScore, bool IsNewBestWave);

// 他のプレイヤーの Id は本人確認を兼ねているので、ランキングには含めない
public record RankingEntryResponse(int Rank, string Name, int BestScore, int BestWave, bool IsMe);

public record RankingResponse(IReadOnlyList<RankingEntryResponse> Entries, RankingEntryResponse? Me);
