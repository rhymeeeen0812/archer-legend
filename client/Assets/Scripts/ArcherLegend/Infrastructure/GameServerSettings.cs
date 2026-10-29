using UnityEngine;

namespace ArcherLegend.Infrastructure
{
    /// <summary>
    /// 接続先のゲームサーバーの設定です。Resources/GameServerSettings に置きます。
    /// </summary>
    [CreateAssetMenu(fileName = "GameServerSettings", menuName = "ArcherLegend/Game Server Settings")]
    public sealed class GameServerSettings : ScriptableObject
    {
        private const string ResourcePath = "GameServerSettings";
        private const string DefaultBaseUrl = "http://localhost:5032";

        [Tooltip("ゲーム用 API の URL です。末尾の / は不要です。")]
        [SerializeField] private string baseUrl = DefaultBaseUrl;

        public string BaseUrl => baseUrl.TrimEnd('/');

        /// <summary>設定アセットがなければ、ローカルで起動したサーバーに接続します。</summary>
        public static string LoadBaseUrl()
        {
            var settings = Resources.Load<GameServerSettings>(ResourcePath);
            return settings != null ? settings.BaseUrl : DefaultBaseUrl;
        }
    }
}
