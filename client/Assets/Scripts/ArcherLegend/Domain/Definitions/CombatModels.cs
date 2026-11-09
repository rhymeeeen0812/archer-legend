namespace ArcherLegend.Domain
{
    public enum UpgradeType
    {
        RapidShot,
        PowerShot,
        SwiftBoots,
        Vitality,
        SpreadShot,
        PiercingShot,
        BouncingShot
    }

    public enum EnemyType
    {
        /// <summary>まっすぐ近づいて体当たりする</summary>
        Chaser,

        /// <summary>距離を取って弾を撃つ</summary>
        Shooter,

        /// <summary>溜めてから一直線に突進する</summary>
        Charger
    }

    public enum ItemType
    {
        Upgrade,
        Consumable,
        Currency
    }

    public readonly struct UpgradeOption
    {
        public UpgradeOption(UpgradeType type, string title, string description)
        {
            Type = type;
            Title = title;
            Description = description;
        }

        public UpgradeType Type { get; }
        public string Title { get; }
        public string Description { get; }
    }
}
