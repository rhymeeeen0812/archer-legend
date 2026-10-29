using System;

namespace ArcherLegend.Domain
{
    /// <summary>
    /// ライフサイクルを持つドメインオブジェクトの共通基底です。
    /// 表示オブジェクトは Unity に破棄されますが、ドメインオブジェクトはセッションから明示的に破棄します。
    /// </summary>
    public abstract class GameEntityBase : IDisposable
    {
        protected GameEntityBase(string id)
        {
            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
        }

        public string Id { get; }
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            OnDispose();
            IsDisposed = true;
        }

        /// <summary>
        /// 子要素、購読、キャッシュなどを持つ具象クラスはここで後始末を実装します。
        /// </summary>
        protected virtual void OnDispose()
        {
        }

        protected void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
