using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ArcherLegend.Application
{
    /// <summary>
    /// ゲーム内アンケートの取得と回答の送信を行うユースケースです。
    /// </summary>
    public sealed class SurveyService
    {
        private readonly PlayerProgressService progress;
        private readonly OnlineSyncService sync;
        private readonly IGameApiClient api;

        public SurveyService(PlayerProgressService progress, OnlineSyncService sync, IGameApiClient api)
        {
            this.progress = progress;
            this.sync = sync;
            this.api = api;
        }

        /// <summary>まだ回答していない、受付中のアンケートを新しい順に返します。</summary>
        public async Task<IReadOnlyList<Survey>> GetAvailableAsync()
        {
            // 取得に必要なのは登録だけ。プレイ結果の送信に失敗していても、アンケートには答えられるようにする
            var profile = await sync.EnsureRegisteredAsync();
            try
            {
                return await api.GetSurveysAsync(profile.Id, CancellationToken.None);
            }
            catch (GameApiException e) when (e.IsNotFound)
            {
                // サーバー側からプレイヤーが消えていた（開発中に DB を作り直した場合など）。登録し直して取得する
                await progress.SetRegisteredAsync(false);
                await sync.EnsureRegisteredAsync();
                return await api.GetSurveysAsync(profile.Id, CancellationToken.None);
            }
        }

        public async Task SubmitAsync(SurveyAnswerSheet sheet)
        {
            var profile = await progress.LoadProfileAsync();
            await api.SubmitSurveyResponseAsync(profile.Id, sheet.Survey.Id, sheet.ToAnswers(), CancellationToken.None);
        }
    }
}
