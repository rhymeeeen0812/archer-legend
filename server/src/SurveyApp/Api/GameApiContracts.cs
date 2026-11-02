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

// ゲーム内アンケート。Unity の JsonUtility は配列を直接読めないので、一覧はオブジェクトで包む
public record GameSurveyListResponse(IReadOnlyList<GameSurveyResponse> Surveys);

public record GameSurveyResponse(Guid Id, string Title, string? Description, IReadOnlyList<GameQuestionResponse> Questions);

// Type は "SingleChoice" / "MultipleChoice" / "FreeText"
public record GameQuestionResponse(int Id, string Text, string Type, bool IsRequired, IReadOnlyList<GameChoiceResponse> Choices);

public record GameChoiceResponse(int Id, string Text);

public record SubmitSurveyResponseRequest(IReadOnlyList<SubmitAnswerRequest>? Answers);

// 選択式は ChoiceIds、自由記述は Text を使う。使わない方は空でよい
public record SubmitAnswerRequest(int QuestionId, int[]? ChoiceIds, string? Text);
