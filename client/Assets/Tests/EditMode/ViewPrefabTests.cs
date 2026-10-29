using System;
using System.Collections;
using System.Reflection;
using ArcherLegend.Presentation;
using ArcherLegend.Presentation.Game;
using ArcherLegend.Presentation.Home;
using ArcherLegend.Presentation.Result;
using ArcherLegend.Presentation.Title;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArcherLegend.Tests
{
    /// <summary>
    /// プレハブをエディタで編集したときに、参照の外れや名前の変更で実行時に落ちないことを確かめます。
    /// </summary>
    public class ViewPrefabTests
    {
        private static readonly Type[] ViewTypes =
        {
            typeof(TitleView),
            typeof(HomeView),
            typeof(GameHudView),
            typeof(ResultView)
        };

        [TestCaseSource(nameof(ViewTypes))]
        public void プレハブが存在しSerializeFieldの参照がすべて設定されている(Type viewType)
        {
            var prefab = Resources.Load($"{ViewPrefabs.ResourceFolder}/{viewType.Name}", viewType);
            Assert.That(prefab, Is.Not.Null, $"Resources/{ViewPrefabs.ResourceFolder}/{viewType.Name}.prefab が見つかりません");

            var fields = viewType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<SerializeField>() == null)
                {
                    continue;
                }

                var value = field.GetValue(prefab);
                if (value is IList list)
                {
                    Assert.That(list.Count, Is.GreaterThan(0), $"{viewType.Name}.{field.Name} が空です");
                    foreach (var element in list)
                    {
                        Assert.That(IsAssigned(element), Is.True, $"{viewType.Name}.{field.Name} に未設定の要素があります");
                    }

                    continue;
                }

                Assert.That(IsAssigned(value), Is.True, $"{viewType.Name}.{field.Name} が未設定です");
            }
        }

        [Test]
        public void GameHudViewの強化ボタンとラベルの数が一致している()
        {
            var prefab = Resources.Load<GameHudView>($"{ViewPrefabs.ResourceFolder}/{nameof(GameHudView)}");
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var buttons = (IList)typeof(GameHudView).GetField("upgradeButtons", flags).GetValue(prefab);
            var labels = (IList)typeof(GameHudView).GetField("upgradeLabels", flags).GetValue(prefab);

            Assert.That(labels.Count, Is.EqualTo(buttons.Count));
        }

        // 参照が外れた UnityEngine.Object は C# の null ではないので、Unity の == 演算子で判定する
        private static bool IsAssigned(object value)
        {
            return value is Object unityObject && unityObject != null;
        }
    }
}
