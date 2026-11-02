using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace SurveyApp.Admin;

public static class AdminEndpoints
{
    public const string LoginRateLimitPolicy = "admin-login";

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // 運営ツールの「CSV をダウンロード」から呼ぶ（ゲーム用 API ではないので OpenAPI には載せない）
        app.MapGet("/admin/surveys/{id:guid}/responses.csv", async (Guid id, SurveyAdminService service) =>
        {
            var file = await service.ExportCsvAsync(id);
            return file is { } csv
                ? Results.File(csv.Content, "text/csv; charset=utf-8", csv.FileName)
                : Results.NotFound();
        }).ExcludeFromDescription();

        app.MapPost("/admin/login", Login)
            .AllowAnonymous()
            .RequireRateLimiting(LoginRateLimitPolicy)
            .ExcludeFromDescription();

        app.MapPost("/admin/logout", Logout).ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> Login(
        HttpContext context, IConfiguration configuration, [FromForm] string? password, [FromForm] string? returnUrl)
    {
        var target = IsLocalPath(returnUrl) ? returnUrl! : "/";

        if (!IsValidPassword(password, configuration["Admin:Password"]))
        {
            return Results.Redirect($"/login?failed=true&returnUrl={Uri.EscapeDataString(target)}");
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Results.LocalRedirect(target);
    }

    private static async Task Logout(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.Response.Redirect("/login");
    }

    // パスワードが未設定の環境では誰もログインできない。長さや一致位置が処理時間に出ないよう、ハッシュを固定時間で比較する
    private static bool IsValidPassword(string? input, string? expected)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(expected))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(input)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
    }

    // ログイン後に外部サイトへ飛ばされないよう、同じサイト内のパスだけを受け付ける
    private static bool IsLocalPath(string? url) =>
        !string.IsNullOrEmpty(url) && url[0] == '/' && !url.StartsWith("//") && !url.StartsWith("/\\");
}
