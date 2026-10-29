namespace ArcherLegend.Domain
{
    public enum UpgradeType
    {
        RapidShot,
        PowerShot,
        SwiftBoots,
        Vitality
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
