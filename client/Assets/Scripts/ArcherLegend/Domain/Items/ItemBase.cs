namespace ArcherLegend.Domain
{
    /// <summary>
    /// ドロップアイテムの基底です。新しいアイテム種別は ApplyTo を実装すれば追加できます。
    /// </summary>
    public abstract class ItemBase : GameEntityBase
    {
        protected ItemBase(string id, ItemType itemType, string displayName) : base(id)
        {
            ItemType = itemType;
            DisplayName = displayName;
        }

        public ItemType ItemType { get; }
        public string DisplayName { get; }

        public abstract void ApplyTo(Player player);
    }
}
