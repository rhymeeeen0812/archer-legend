using System.Threading;
using System.Threading.Tasks;

namespace ArcherLegend.Application
{
    /// <summary>
    /// セーブデータの保存先です。今はローカルファイルに保存し、サーバー連携時に実装を差し替えます。
    /// 通信を見越して非同期のインターフェースにしています。
    /// </summary>
    public interface ISaveDataRepository
    {
        /// <returns>保存されたデータがなければ null</returns>
        Task<SaveData> LoadAsync(CancellationToken cancellationToken);

        Task SaveAsync(SaveData data, CancellationToken cancellationToken);
    }
}
