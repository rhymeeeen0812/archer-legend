using ArcherLegend.Presentation.Game;
using NUnit.Framework;
using UnityEngine;

namespace ArcherLegend.Tests
{
    public class ArenaRulesTests
    {
        [Test]
        public void IsHit_1フレームで敵を飛び越えても命中する()
        {
            var hit = ArrowView.IsHit(new Vector3(-2f, 0.85f, 0f), new Vector3(2f, 0.85f, 0f), new Vector3(0f, 0.55f, 0f), 0.38f);

            Assert.That(hit, Is.True);
        }

        [Test]
        public void IsHit_敵から離れた線分や敵に届く前の線分は命中しない()
        {
            var enemy = Vector3.zero;

            Assert.That(ArrowView.IsHit(new Vector3(-2f, 0f, 1f), new Vector3(2f, 0f, 1f), enemy, 0.38f), Is.False);
            Assert.That(ArrowView.IsHit(new Vector3(-4f, 0f, 0f), new Vector3(-2f, 0f, 0f), enemy, 0.38f), Is.False);
        }

        [Test]
        public void IsHit_矢が止まっていても敵に重なっていれば命中する()
        {
            var position = new Vector3(0.1f, 0f, 0f);

            Assert.That(ArrowView.IsHit(position, position, Vector3.zero, 0.38f), Is.True);
        }

        [Test]
        public void ResolveSpawnPosition_プレイヤーの近くなら反対側へ移す()
        {
            var candidate = new Vector3(7.6f, 0.55f, 0f);

            var position = ArcherGameController.ResolveSpawnPosition(candidate, new Vector3(7.6f, 0.6f, 0f));

            Assert.That(position, Is.EqualTo(new Vector3(-7.6f, 0.55f, 0f)));
        }

        [Test]
        public void ResolveSpawnPosition_離れていればそのままの位置に出す()
        {
            var candidate = new Vector3(7.6f, 0.55f, 0f);

            var position = ArcherGameController.ResolveSpawnPosition(candidate, new Vector3(0f, 0.6f, 0f));

            Assert.That(position, Is.EqualTo(candidate));
        }
    }
}
