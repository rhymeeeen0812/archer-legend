namespace ArcherLegend.Domain
{
    /// <summary>
    /// アイテム具象例です。将来はレベルアップ選択だけでなく、ドロップとして生成できます。
    /// </summary>
    public sealed class UpgradeItem : ItemBase
    {
        public UpgradeItem(UpgradeType upgradeType) : base($"upgrade-{upgradeType}", ItemType.Upgrade, upgradeType.ToString())
        {
            UpgradeType = upgradeType;
        }

        public UpgradeType UpgradeType { get; }

        public override void ApplyTo(Player player)
        {
            ThrowIfDisposed();
            player.ApplyUpgrade(UpgradeType);
        }
    }
}
