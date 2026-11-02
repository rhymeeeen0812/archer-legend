using SurveyApp.Data;

namespace SurveyApp.Admin;

// 運営ツールの画面に出す日本語の表記
public static class SurveyLabels
{
    public static string Status(SurveyStatus status) => status switch
    {
        SurveyStatus.Draft => "下書き",
        SurveyStatus.Published => "公開中",
        SurveyStatus.Closed => "終了",
        _ => status.ToString(),
    };

    public static string StatusBadgeClass(SurveyStatus status) => status switch
    {
        SurveyStatus.Draft => "text-bg-secondary",
        SurveyStatus.Published => "text-bg-success",
        _ => "text-bg-dark",
    };

    public static string Type(QuestionType type) => type switch
    {
        QuestionType.SingleChoice => "単一選択",
        QuestionType.MultipleChoice => "複数選択",
        QuestionType.FreeText => "自由記述",
        _ => type.ToString(),
    };

    public static string DateTime(DateTimeOffset? value) =>
        value is { } v
            ? TimeZoneInfo.ConvertTime(v, SurveyAdminService.OpsTimeZone).ToString("yyyy/MM/dd HH:mm")
            : "期限なし";
}
