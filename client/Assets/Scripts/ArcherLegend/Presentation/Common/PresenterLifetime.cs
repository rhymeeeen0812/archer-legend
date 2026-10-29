using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcherLegend.Presentation
{
    /// <summary>
    /// Presenter は MonoBehaviour ではないため、紐づく View の GameObject が破棄されたときに一緒に Dispose します。
    /// </summary>
    public sealed class PresenterLifetime : MonoBehaviour
    {
        private readonly List<IDisposable> disposables = new List<IDisposable>();

        public static void Bind(GameObject owner, IDisposable presenter)
        {
            if (!owner.TryGetComponent<PresenterLifetime>(out var lifetime))
            {
                lifetime = owner.AddComponent<PresenterLifetime>();
            }

            lifetime.disposables.Add(presenter);
        }

        private void OnDestroy()
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }

            disposables.Clear();
        }
    }
}
