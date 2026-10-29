using System.Net;
using System.Net.Http.Json;
using SurveyApp.Api;

namespace SurveyApp.Tests.Api;

[Collection(GameApiCollection.Name)]
public class GameApiTests(GameApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task 登録したプレイヤーを取得できる()
    {
        var id = Guid.NewGuid();

        var created = await client.PostAsJsonAsync("/api/players", new RegisterPlayerRequest(id, " アーチャー "));
        var player = await client.GetFromJsonAsync<PlayerResponse>($"/api/players/{id}");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("アーチャー", player!.Name);
        Assert.Equal(new PlayerRecordsResponse(0, 0, 0, 0, 0), player.Records);
    }

    [Fact]
    public async Task 同じIdで再登録すると登録済みのプレイヤーを返す()
    {
        var id = Guid.NewGuid();
        await client.PostAsJsonAsync("/api/players", new RegisterPlayerRequest(id, "最初の名前"));

        var again = await client.PostAsJsonAsync("/api/players", new RegisterPlayerRequest(id, "別の名前"));
        var player = await again.Content.ReadFromJsonAsync<PlayerResponse>();

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal("最初の名前", player!.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456789012345678901")]
    public async Task 名前が不正なら400を返す(string name)
    {
        var response = await client.PostAsJsonAsync("/api/players", new RegisterPlayerRequest(Guid.NewGuid(), name));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task 存在しないプレイヤーは画面ではなくAPIとして404を返す()
    {
        var response = await client.GetAsync($"/api/players/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task 名前を変更できる()
    {
        var id = await RegisterAsync("変更前");

        var response = await client.PutAsJsonAsync($"/api/players/{id}/name", new RenamePlayerRequest("変更後"));
        var player = await client.GetFromJsonAsync<PlayerResponse>($"/api/players/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("変更後", player!.Name);
    }

    [Fact]
    public async Task プレイ結果を送ると戦績が更新される()
    {
        var id = await RegisterAsync("アーチャー");

        var first = await SubmitRunAsync(id, score: 100, wave: 3, enemiesDefeated: 8);
        var second = await SubmitRunAsync(id, score: 60, wave: 4, enemiesDefeated: 5);

        Assert.True(first.IsNewBestScore);
        Assert.False(second.IsNewBestScore);
        Assert.True(second.IsNewBestWave);
        Assert.Equal(new PlayerRecordsResponse(100, 4, 160, 13, 2), second.Records);
    }

    [Fact]
    public async Task 同じRunIdの再送は二重に記録せず最初と同じ結果を返す()
    {
        var id = await RegisterAsync("アーチャー");
        var request = new SubmitRunRequest(Guid.NewGuid(), 100, 3, 8, 2);

        var first = await (await client.PostAsJsonAsync($"/api/players/{id}/runs", request)).Content.ReadFromJsonAsync<SubmitRunResponse>();
        var retried = await (await client.PostAsJsonAsync($"/api/players/{id}/runs", request)).Content.ReadFromJsonAsync<SubmitRunResponse>();

        Assert.Equal(first, retried);
        Assert.Equal(1, retried!.Records.PlayCount);
        Assert.True(retried.IsNewBestScore);
    }

    [Fact]
    public async Task 別のプレイヤーのRunIdを使うと409を返す()
    {
        var runId = Guid.NewGuid();
        var owner = await RegisterAsync("持ち主");
        var other = await RegisterAsync("別の人");
        await client.PostAsJsonAsync($"/api/players/{owner}/runs", new SubmitRunRequest(runId, 100, 3, 8, 2));

        var response = await client.PostAsJsonAsync($"/api/players/{other}/runs", new SubmitRunRequest(runId, 100, 3, 8, 2));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task 存在しないプレイヤーのプレイ結果は404を返す()
    {
        var response = await client.PostAsJsonAsync($"/api/players/{Guid.NewGuid()}/runs", new SubmitRunRequest(Guid.NewGuid(), 100, 3, 8, 2));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(-1, 1, 0, 1)]
    [InlineData(0, 0, 0, 1)]
    [InlineData(0, 1, -1, 1)]
    [InlineData(0, 1, 0, 0)]
    public async Task プレイ結果の値が範囲外なら400を返す(int score, int wave, int enemiesDefeated, int level)
    {
        var id = await RegisterAsync("アーチャー");

        var response = await client.PostAsJsonAsync($"/api/players/{id}/runs", new SubmitRunRequest(Guid.NewGuid(), score, wave, enemiesDefeated, level));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ランキングはスコア順で同点なら先に達成したプレイヤーが上位になる()
    {
        var early = await RegisterAsync("先に達成");
        var late = await RegisterAsync("後に達成");
        var top = await RegisterAsync("トップ");
        await RegisterAsync("未プレイ");

        await SubmitRunAsync(early, score: 100, wave: 3, enemiesDefeated: 8);
        factory.Time.Advance(TimeSpan.FromMinutes(1));
        await SubmitRunAsync(late, score: 100, wave: 5, enemiesDefeated: 8);
        await SubmitRunAsync(top, score: 300, wave: 6, enemiesDefeated: 20);

        var ranking = await client.GetFromJsonAsync<RankingResponse>("/api/rankings");

        Assert.Equal(["トップ", "先に達成", "後に達成"], ranking!.Entries.Select(e => e.Name));
        Assert.Equal([1, 2, 3], ranking.Entries.Select(e => e.Rank));
        Assert.Null(ranking.Me);
    }

    [Fact]
    public async Task 上位に入っていなくても自分の順位を返し他人のIdは含めない()
    {
        var others = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            var other = await RegisterAsync($"上位{i}");
            await SubmitRunAsync(other, score: 1000 + i, wave: 5, enemiesDefeated: 10);
            others.Add(other);
        }

        var me = await RegisterAsync("自分");
        await SubmitRunAsync(me, score: 10, wave: 1, enemiesDefeated: 1);

        var response = await client.GetAsync($"/api/rankings?playerId={me}&limit=2");
        var json = await response.Content.ReadAsStringAsync();
        var ranking = await response.Content.ReadFromJsonAsync<RankingResponse>();

        Assert.Equal(2, ranking!.Entries.Count);
        Assert.DoesNotContain(ranking.Entries, e => e.IsMe);
        Assert.Equal(new RankingEntryResponse(4, "自分", 10, 1, true), ranking.Me);
        Assert.All(others, other => Assert.DoesNotContain(other.ToString(), json));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task ランキングの件数が範囲外なら400を返す(int limit)
    {
        var response = await client.GetAsync($"/api/rankings?limit={limit}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> RegisterAsync(string name)
    {
        var id = Guid.NewGuid();
        var response = await client.PostAsJsonAsync("/api/players", new RegisterPlayerRequest(id, name));
        response.EnsureSuccessStatusCode();
        return id;
    }

    private async Task<SubmitRunResponse> SubmitRunAsync(Guid playerId, int score, int wave, int enemiesDefeated)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/players/{playerId}/runs",
            new SubmitRunRequest(Guid.NewGuid(), score, wave, enemiesDefeated, Level: 1));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SubmitRunResponse>())!;
    }
}
