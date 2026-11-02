using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ArcherLegend.Application
{
    /// <summary>
    /// ゲーム用 API（server/）との通信です。通信手段（UnityWebRequest など）は Infrastructure 層で実装します。
    /// 失敗したときは <see cref="GameApiException"/> を投げます。
    /// </summary>
    public interface IGameApiClient
    {
        /// <summary>同じ Id で何度呼んでも 1 人として登録されます。</summary>
        Task RegisterPlayerAsync(string playerId, string name, CancellationToken cancellationToken);

        /// <summary>同じ runId で再送しても二重に記録されません。</summary>
        Task SubmitRunAsync(string playerId, PendingRun run, CancellationToken cancellationToken);

        Task<Ranking> GetRankingAsync(string playerId, int limit, CancellationToken cancellationToken);

        /// <summary>まだ回答していない、受付中のアンケートを返します。</summary>
        Task<IReadOnlyList<Survey>> GetSurveysAsync(string playerId, CancellationToken cancellationToken);

        /// <summary>回答済み・受付終了のときは 409 の <see cref="GameApiException"/> を投げます。</summary>
        Task SubmitSurveyResponseAsync(string playerId, string surveyId, IReadOnlyList<SurveyAnswer> answers, CancellationToken cancellationToken);
    }
}
