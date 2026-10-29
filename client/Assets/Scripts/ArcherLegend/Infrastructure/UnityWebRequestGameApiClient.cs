using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArcherLegend.Application;
using UnityEngine;
using UnityEngine.Networking;

namespace ArcherLegend.Infrastructure
{
    /// <summary>
    /// UnityWebRequest でゲーム用 API を呼び出します。WebGL でも動くよう、スレッドは使わずに完了を待ちます。
    /// </summary>
    public sealed class UnityWebRequestGameApiClient : IGameApiClient
    {
        private const int TimeoutSeconds = 10;

        private readonly string baseUrl;

        public UnityWebRequestGameApiClient(string baseUrl)
        {
            this.baseUrl = baseUrl;
        }

        public Task RegisterPlayerAsync(string playerId, string name, CancellationToken cancellationToken)
        {
            var body = new RegisterPlayerRequest { id = playerId, name = name };
            return SendAsync("POST", "/api/players", JsonUtility.ToJson(body), cancellationToken);
        }

        public Task SubmitRunAsync(string playerId, PendingRun run, CancellationToken cancellationToken)
        {
            // PendingRun はサーバーの SubmitRunRequest と同じ項目名なので、そのまま送る
            return SendAsync("POST", $"/api/players/{Uri.EscapeDataString(playerId)}/runs", JsonUtility.ToJson(run), cancellationToken);
        }

        public async Task<Ranking> GetRankingAsync(string playerId, int limit, CancellationToken cancellationToken)
        {
            var json = await SendAsync("GET", $"/api/rankings?playerId={Uri.EscapeDataString(playerId)}&limit={limit}", null, cancellationToken);
            var response = JsonUtility.FromJson<RankingResponse>(json);

            var entries = new RankingEntry[response.entries?.Length ?? 0];
            for (var i = 0; i < entries.Length; i++)
            {
                entries[i] = response.entries[i].ToEntry();
            }

            // JsonUtility は null のオブジェクトも空のインスタンスとして読み込むので、順位の有無で判定する
            var me = response.me != null && response.me.rank > 0 ? response.me.ToEntry() : (RankingEntry?)null;
            return new Ranking(entries, me);
        }

        private async Task<string> SendAsync(string method, string path, string jsonBody, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var request = new UnityWebRequest(baseUrl + path, method))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                if (jsonBody != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                    request.SetRequestHeader("Content-Type", "application/json");
                }

                request.timeout = TimeoutSeconds;

                var completion = new TaskCompletionSource<bool>();
                request.SendWebRequest().completed += _ => completion.TrySetResult(true);
                using (cancellationToken.Register(request.Abort))
                {
                    await completion.Task;
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new GameApiException(request.responseCode, $"{method} {path} failed: {request.error}");
                }

                return request.downloadHandler.text;
            }
        }

        // サーバーとやり取りする JSON の形（JsonUtility で扱うため public フィールドにしている）

        [Serializable]
        private sealed class RegisterPlayerRequest
        {
            public string id;
            public string name;
        }

        [Serializable]
        private sealed class RankingResponse
        {
            public RankingEntryResponse[] entries;
            public RankingEntryResponse me;
        }

        [Serializable]
        private sealed class RankingEntryResponse
        {
            public int rank;
            public string name;
            public int bestScore;
            public int bestWave;
            public bool isMe;

            public RankingEntry ToEntry() => new RankingEntry(rank, name, bestScore, bestWave, isMe);
        }
    }
}
