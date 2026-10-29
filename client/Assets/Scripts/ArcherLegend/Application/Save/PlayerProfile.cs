namespace ArcherLegend.Application
{
    /// <summary>
    /// サーバーでこのプレイヤーを識別するための情報です。ログインは行わず、初回起動時に作った Id を使い続けます。
    /// </summary>
    public sealed class PlayerProfile
    {
        public PlayerProfile(string id, string name, bool isRegistered)
        {
            Id = id;
            Name = name;
            IsRegistered = isRegistered;
        }

        public string Id { get; }
        public string Name { get; }

        /// <summary>サーバーへの登録が済んでいるかどうかです。</summary>
        public bool IsRegistered { get; internal set; }
    }
}
