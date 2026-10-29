using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArcherLegend.Presentation
{
    /// <summary>
    /// 画面のプレハブを Resources/UI から読み込んで生成します。プレハブ名は View のクラス名と揃えています。
    /// </summary>
    public static class ViewPrefabs
    {
        public const string ResourceFolder = "UI";

        public static T Instantiate<T>() where T : MonoBehaviour
        {
            var path = $"{ResourceFolder}/{typeof(T).Name}";
            var prefab = Resources.Load<T>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException($"プレハブ Resources/{path}.prefab が見つかりません。");
            }

            var view = Object.Instantiate(prefab);
            view.name = prefab.name;
            return view;
        }
    }
}
