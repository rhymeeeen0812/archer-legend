using SurveyApp.Data;

namespace SurveyApp.Tests;

public class PlayerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    private static Player CreatePlayer() => new() { Id = Guid.NewGuid(), Name = "テスト" };

    [Fact]
    public void RecordRun_自己ベストを超えたら更新して達成日時を記録する()
    {
        var player = CreatePlayer();

        var (isNewBestScore, isNewBestWave) = player.RecordRun(score: 120, wave: 3, enemiesDefeated: 9, Now);

        Assert.True(isNewBestScore);
        Assert.True(isNewBestWave);
        Assert.Equal(120, player.BestScore);
        Assert.Equal(3, player.BestWave);
        Assert.Equal(Now, player.BestScoreAchievedAt);
    }

    [Fact]
    public void RecordRun_自己ベスト以下なら更新せず累計だけ増やす()
    {
        var player = CreatePlayer();
        player.RecordRun(score: 120, wave: 3, enemiesDefeated: 9, Now);

        var (isNewBestScore, isNewBestWave) = player.RecordRun(score: 120, wave: 2, enemiesDefeated: 5, Now.AddMinutes(5));

        Assert.False(isNewBestScore);
        Assert.False(isNewBestWave);
        Assert.Equal(120, player.BestScore);
        Assert.Equal(Now, player.BestScoreAchievedAt);
        Assert.Equal(240, player.TotalScore);
        Assert.Equal(14, player.TotalEnemiesDefeated);
        Assert.Equal(2, player.PlayCount);
    }

    [Theory]
    [InlineData("アーチャー", "アーチャー")]
    [InlineData("  前後に空白  ", "前後に空白")]
    [InlineData("12345678901234567890", "12345678901234567890")]
    public void TryNormalizeName_有効な名前は前後の空白を除いて受け付ける(string input, string expected)
    {
        Assert.True(Player.TryNormalizeName(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123456789012345678901")]
    [InlineData("改行\nあり")]
    public void TryNormalizeName_空や長すぎる名前や制御文字は受け付けない(string? input)
    {
        Assert.False(Player.TryNormalizeName(input, out _));
    }
}
