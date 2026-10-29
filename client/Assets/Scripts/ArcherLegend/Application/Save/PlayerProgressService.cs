using System.Threading;
using System.Threading.Tasks;
using ArcherLegend.Domain;

namespace ArcherLegend.Application
{
    /// <summary>
    /// シーンをまたいで 1 つの UserData を共有し、保存先とのやり取りをまとめるユースケースです。
    /// </summary>
    public sealed class PlayerProgressService
    {
        private readonly ISaveDataRepository repository;
        private Task<UserData> loadTask;

        public PlayerProgressService(ISaveDataRepository repository)
        {
            this.repository = repository;
        }

        /// <summary>
        /// 初回だけ保存先から読み込み、以降は同じ UserData を返します。読み込みに失敗した場合は次回呼び出し時に再試行します。
        /// </summary>
        public Task<UserData> LoadAsync()
        {
            if (loadTask == null || loadTask.IsFaulted || loadTask.IsCanceled)
            {
                loadTask = LoadCoreAsync();
            }

            return loadTask;
        }

        public async Task SaveAsync()
        {
            var userData = await LoadAsync();
            await repository.SaveAsync(SaveData.FromUserData(userData), CancellationToken.None);
        }

        private async Task<UserData> LoadCoreAsync()
        {
            var data = await repository.LoadAsync(CancellationToken.None);
            return data?.ToUserData() ?? new UserData();
        }
    }
}
