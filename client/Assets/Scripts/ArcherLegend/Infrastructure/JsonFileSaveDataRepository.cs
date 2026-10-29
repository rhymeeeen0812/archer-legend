using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ArcherLegend.Application;
using UnityEngine;

namespace ArcherLegend.Infrastructure
{
    /// <summary>
    /// セーブデータを JSON ファイルに保存します。
    /// WebGL ではスレッドを使えないため、ファイル操作は同期で行い、完了済みの Task で返します。
    /// </summary>
    public sealed class JsonFileSaveDataRepository : ISaveDataRepository
    {
        private readonly string filePath;
        private readonly string tempFilePath;

        public JsonFileSaveDataRepository(string directory)
        {
            filePath = Path.Combine(directory, "save.json");
            tempFilePath = filePath + ".tmp";
        }

        public Task<SaveData> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 置き換えの途中で終了した場合は、書き終えていた一時ファイルから復旧する
            var path = File.Exists(filePath) ? filePath : tempFilePath;
            if (!File.Exists(path))
            {
                return Task.FromResult<SaveData>(null);
            }

            try
            {
                return Task.FromResult(JsonUtility.FromJson<SaveData>(File.ReadAllText(path)));
            }
            catch (ArgumentException e)
            {
                // 壊れたファイルのせいで遊べなくなるのを避け、退避してから新しいデータで始める
                Debug.LogWarning($"Save data is broken and was moved aside: {e.Message}");
                File.Copy(path, path + ".broken", true);
                File.Delete(path);
                return Task.FromResult<SaveData>(null);
            }
        }

        public Task SaveAsync(SaveData data, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // 一時ファイルに書き切ってから置き換え、書き込み中に終了してもファイルが壊れないようにする
            File.WriteAllText(tempFilePath, JsonUtility.ToJson(data));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.Move(tempFilePath, filePath);
            return Task.CompletedTask;
        }
    }
}
