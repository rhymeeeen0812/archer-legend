using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SurveyApp.Admin;
using SurveyApp.Api;
using SurveyApp.Data;

namespace SurveyApp.Tests.Api;

// 運営ツールで作ったアンケートに、ゲームから回答して集計されるまでの流れを確かめる
[Collection(GameApiCollection.Name)]
public class SurveyApiTests(GameApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient client = factory.CreateClient();
    private IServiceScope scope = null!;
    private SurveyAdminService admin = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetAsync();
        scope = factory.Services.CreateScope();
        admin = scope.ServiceProvider.GetRequiredService<SurveyAdminService>();
    }

    public Task DisposeAsync()
    {
        scope.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task 公開中で締め切り前の未回答のアンケートだけ取得できる()
    {
        var player = await RegisterAsync();
        await CreateSurveyAsync("下書き", publish: false);
        var closed = await CreateSurveyAsync("終了");
        await admin.CloseAsync(closed);
        await CreateSurveyAsync("締め切り済み", closesAt: factory.Time.GetUtcNow().AddHours(1));
        factory.Time.Advance(TimeSpan.FromHours(2));
        var open = await CreateSurveyAsync("公開中");

        var list = await client.GetFromJsonAsync<GameSurveyListResponse>($"/api/players/{player}/surveys");

        var survey = Assert.Single(list!.Surveys);
        Assert.Equal(open, survey.Id);
        Assert.Equal(["SingleChoice", "MultipleChoice", "FreeText"], survey.Questions.Select(q => q.Type));
        Assert.Equal(["とても楽しい", "楽しい", "ふつう"], survey.Questions[0].Choices.Select(c => c.Text));
    }

    [Fact]
    public async Task 回答すると集計に反映され一覧から消える()
    {
        var player = await RegisterAsync();
        var surveyId = await CreateSurveyAsync("満足度調査");
        var survey = await GetGameSurveyAsync(player);

        var response = await SubmitAsync(player, surveyId,
            new SubmitAnswerRequest(survey.Questions[0].Id, [survey.Questions[0].Choices[1].Id], ""),
            new SubmitAnswerRequest(survey.Questions[1].Id, [survey.Questions[1].Choices[0].Id, survey.Questions[1].Choices[1].Id], ""),
            new SubmitAnswerRequest(survey.Questions[2].Id, [], "もっと敵を増やしてほしい"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var results = (await admin.GetResultsAsync(surveyId))!;
        Assert.Equal(1, results.ResponseCount);
        Assert.Equal([0, 1, 0], results.Questions[0].Choices.Select(c => c.Count));
        Assert.Equal([1, 1], results.Questions[1].Choices.Select(c => c.Count));
        Assert.Equal(1, results.Questions[1].AnsweredCount);
        Assert.Equal(["もっと敵を増やしてほしい"], results.Questions[2].TextAnswers);

        var list = await client.GetFromJsonAsync<GameSurveyListResponse>($"/api/players/{player}/surveys");
        Assert.Empty(list!.Surveys);
    }

    [Fact]
    public async Task 同じアンケートに2回回答すると409を返す()
    {
        var player = await RegisterAsync();
        var surveyId = await CreateSurveyAsync("満足度調査");
        var survey = await GetGameSurveyAsync(player);
        var answer = new SubmitAnswerRequest(survey.Questions[0].Id, [survey.Questions[0].Choices[0].Id], "");
        await SubmitAsync(player, surveyId, answer);

        var again = await SubmitAsync(player, surveyId, answer);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task 受付終了後の回答は409を返す()
    {
        var player = await RegisterAsync();
        var surveyId = await CreateSurveyAsync("満足度調査");
        var survey = await GetGameSurveyAsync(player);
        await admin.CloseAsync(surveyId);

        var response = await SubmitAsync(player, surveyId, new SubmitAnswerRequest(survey.Questions[0].Id, [survey.Questions[0].Choices[0].Id], ""));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task 必須の質問が未回答なら400を返す()
    {
        var player = await RegisterAsync();
        var surveyId = await CreateSurveyAsync("満足度調査");

        var response = await SubmitAsync(player, surveyId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task 下書きのアンケートには回答できない()
    {
        var player = await RegisterAsync();
        var surveyId = await CreateSurveyAsync("下書き", publish: false);

        var response = await SubmitAsync(player, surveyId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task 下書きは質問を並べ替えて保存し直せるが公開後は編集できない()
    {
        var surveyId = await CreateSurveyAsync("満足度調査", publish: false);
        var form = SurveyAdminService.ToForm((await admin.GetAsync(surveyId))!);
        form.Questions.Reverse();

        var (savedId, errors) = await admin.SaveDraftAsync(surveyId, form);
        Assert.Empty(errors);
        Assert.Equal(QuestionType.FreeText, (await admin.GetAsync(surveyId))!.Questions.OrderBy(q => q.Order).First().Type);

        Assert.Null(await admin.PublishAsync(savedId!.Value));
        var (_, publishedErrors) = await admin.SaveDraftAsync(surveyId, form);
        Assert.NotEmpty(publishedErrors);
    }

    [Fact]
    public async Task 締め切りが過ぎた下書きは公開できない()
    {
        var surveyId = await CreateSurveyAsync("満足度調査", publish: false, closesAt: factory.Time.GetUtcNow().AddMinutes(-1));

        Assert.NotNull(await admin.PublishAsync(surveyId));
    }

    [Fact]
    public async Task CSVは1回答1行で区切り文字や数式に見える値を無害化する()
    {
        var player = await RegisterAsync("=HYPERLINK(\"x\")");
        var surveyId = await CreateSurveyAsync("満足度調査");
        var survey = await GetGameSurveyAsync(player);
        await SubmitAsync(player, surveyId,
            new SubmitAnswerRequest(survey.Questions[0].Id, [survey.Questions[0].Choices[0].Id], ""),
            new SubmitAnswerRequest(survey.Questions[2].Id, [], "楽しい, でも難しい"));

        await LoginAsync(client, GameApiFactory.AdminPassword);
        var response = await client.GetAsync($"/admin/surveys/{surveyId}/responses.csv");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var lines = Encoding.UTF8.GetString(bytes).TrimStart('﻿').Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("回答日時,プレイヤー名,Q1. ", lines[0]);
        Assert.Contains("\"'=HYPERLINK(\"\"x\"\")\"", lines[1]);
        Assert.EndsWith(",とても楽しい,,\"楽しい, でも難しい\"", lines[1]);
    }

    [Fact]
    public async Task 回答にnullの要素があると400を返す()
    {
        var player = await RegisterAsync();
        var surveyId = await CreateSurveyAsync("満足度調査");

        var response = await client.PostAsync(
            $"/api/players/{player}/surveys/{surveyId}/responses",
            new StringContent("{\"answers\":[null]}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/surveys")]
    [InlineData("/_blazor/negotiate")]
    public async Task 未ログインでは運営ツールを開けない(string path)
    {
        var anonymous = CreateClientWithoutRedirect();

        var response = await anonymous.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task 未ログインではCSVをダウンロードできない()
    {
        var surveyId = await CreateSurveyAsync("満足度調査");
        var anonymous = CreateClientWithoutRedirect();

        var response = await anonymous.GetAsync($"/admin/surveys/{surveyId}/responses.csv");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task パスワードが違うとログインできず運営ツールを開けない()
    {
        var anonymous = CreateClientWithoutRedirect();

        var login = await LoginAsync(anonymous, "wrong-password");
        var page = await anonymous.GetAsync("/surveys");

        Assert.Contains("failed=true", login.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
    }

    [Fact]
    public async Task 正しいパスワードでログインすると運営ツールを開ける()
    {
        var authenticated = CreateClientWithoutRedirect();

        var login = await LoginAsync(authenticated, GameApiFactory.AdminPassword, returnUrl: "https://evil.example/");
        var page = await authenticated.GetAsync("/surveys");

        Assert.Equal("/", login.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
    }

    [Fact]
    public async Task ログアウトするとログイン画面に戻り運営ツールを開けなくなる()
    {
        var authenticated = CreateClientWithoutRedirect();
        await LoginAsync(authenticated, GameApiFactory.AdminPassword);
        var token = FindAntiforgeryToken(await authenticated.GetStringAsync("/surveys"));

        var logout = await authenticated.PostAsync("/admin/logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        }));
        var page = await authenticated.GetAsync("/surveys");

        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal("/login", logout.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
    }

    [Fact]
    public async Task ログインなしでもゲーム用APIと静的ファイルは使えて存在しないAPIは404を返す()
    {
        var anonymous = CreateClientWithoutRedirect();

        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/api/rankings")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/_framework/blazor.web.js")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("/api/unknown")).StatusCode);
    }

    private HttpClient CreateClientWithoutRedirect() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient target, string password, string returnUrl = "/")
    {
        var token = FindAntiforgeryToken(await target.GetStringAsync("/login"));

        return await target.PostAsync("/admin/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["password"] = password,
            ["returnUrl"] = returnUrl,
        }));
    }

    private static string FindAntiforgeryToken(string html) =>
        WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);

    private async Task<Guid> RegisterAsync(string name = "アーチャー")
    {
        var id = Guid.NewGuid();
        (await client.PostAsJsonAsync("/api/players", new RegisterPlayerRequest(id, name))).EnsureSuccessStatusCode();
        return id;
    }

    private async Task<Guid> CreateSurveyAsync(string title, bool publish = true, DateTimeOffset? closesAt = null)
    {
        var form = new SurveyForm
        {
            Title = title,
            ClosesAtLocal = closesAt is { } c ? TimeZoneInfo.ConvertTime(c, SurveyAdminService.OpsTimeZone).DateTime : null,
            Questions =
            [
                new QuestionForm
                {
                    Text = "楽しかったですか？",
                    Type = QuestionType.SingleChoice,
                    Choices = [new() { Text = "とても楽しい" }, new() { Text = "楽しい" }, new() { Text = "ふつう" }],
                },
                new QuestionForm
                {
                    Text = "好きな強化は？",
                    Type = QuestionType.MultipleChoice,
                    IsRequired = false,
                    Choices = [new() { Text = "連射" }, new() { Text = "強弓" }],
                },
                new QuestionForm { Text = "ご意見をどうぞ", Type = QuestionType.FreeText, IsRequired = false },
            ],
        };

        var (id, errors) = await admin.SaveDraftAsync(null, form);
        Assert.Empty(errors);
        if (publish)
        {
            Assert.Null(await admin.PublishAsync(id!.Value));
        }

        return id!.Value;
    }

    private async Task<GameSurveyResponse> GetGameSurveyAsync(Guid player)
    {
        var list = await client.GetFromJsonAsync<GameSurveyListResponse>($"/api/players/{player}/surveys");
        return list!.Surveys[0];
    }

    private Task<HttpResponseMessage> SubmitAsync(Guid player, Guid surveyId, params SubmitAnswerRequest[] answers) =>
        client.PostAsJsonAsync($"/api/players/{player}/surveys/{surveyId}/responses", new SubmitSurveyResponseRequest(answers));
}
