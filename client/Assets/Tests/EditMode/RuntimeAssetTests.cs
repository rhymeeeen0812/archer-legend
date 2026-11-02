using ArcherLegend.Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace ArcherLegend.Tests
{
    /// <summary>
    /// エディタでは動くのにビルドでは見つからなくなる素材を、Resources から参照できていることを確かめます。
    /// </summary>
    public class RuntimeAssetTests
    {
        [Test]
        public void ゲーム内のマテリアルの元になる素材がURPのLitシェーダーを参照している()
        {
            var material = Resources.Load<Material>(UnityObjectFactory.BaseMaterialPath);

            Assert.That(material, Is.Not.Null);
            Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
        }
    }
}
