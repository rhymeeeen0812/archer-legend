using UnityEngine;

namespace ArcherLegend.Domain
{
    /// <summary>
    /// ダメージを受ける登場キャラクターの共通基底です。プレイヤーと敵の重複をここへ集約します。
    /// </summary>
    public abstract class CombatantBase : GameEntityBase
    {
        protected CombatantBase(string id, int maxHealth) : base(id)
        {
            MaxHealth = Mathf.Max(1, maxHealth);
            Health = MaxHealth;
        }

        public int MaxHealth { get; protected set; }
        public int Health { get; protected set; }
        public bool IsAlive => Health > 0 && !IsDisposed;

        public virtual void TakeDamage(int amount)
        {
            ThrowIfDisposed();
            Health = Mathf.Max(0, Health - Mathf.Max(0, amount));
        }

        public void Heal(int amount)
        {
            ThrowIfDisposed();
            Health = Mathf.Min(MaxHealth, Health + Mathf.Max(0, amount));
        }

        public void HealToFull()
        {
            ThrowIfDisposed();
            Health = MaxHealth;
        }
    }
}
