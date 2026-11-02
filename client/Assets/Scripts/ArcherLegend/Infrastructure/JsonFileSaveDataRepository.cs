using System;
using System.IO;
using System.Text;
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

            // 本体が読めないときや、置き換えの途中で終了した場合は、書き終えていた一時ファイルから復旧する
            var data = TryLoad(filePath) ?? TryLoad(tempFilePath);
            return Task.FromResult(data);
        }

        private static SaveData TryLoad(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                // 空のファイルは例外にならず null が返るので、壊れたファイルとして同じように扱う
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data != null)
                {
                    return data;
                }

                MoveAside(path, "the file is empty");
            }
            catch (ArgumentException e)
            {
                MoveAside(path, e.Message);
            }

            return null;
        }

        // 壊れたファイルのせいで遊べなくなるのを避け、退避してから新しいデータで始める
        private static void MoveAside(string path, string reason)
        {
            Debug.LogWarning($"Save data is broken and was moved aside: {reason}");
            File.Copy(path, path + ".broken", true);
            File.Delete(path);
        }

        public Task SaveAsync(SaveData data, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // 一時ファイルに書き切ってから置き換え、書き込み中に終了してもファイルが壊れないようにする
            using (var stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            {
                var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
                stream.Write(bytes, 0, bytes.Length);

                // 電源断でも中身が残るよう、置き換える前にディスクまで書き出す
                stream.Flush(true);
            }

            if (File.Exists(filePath))
            {
                File.Replace(tempFilePath, filePath, null);
            }
            else
            {
                File.Move(tempFilePath, filePath);
            }

            return Task.CompletedTask;
        }
    }
}
