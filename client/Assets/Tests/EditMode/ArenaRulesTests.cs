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
        public void SpreadDirections_1本なら狙った方向に放つ()
        {
            var directions = ArrowView.SpreadDirections(new Vector3(0f, 0f, 2f), 1, 15f);

            Assert.That(directions, Has.Length.EqualTo(1));
            Assert.That(Vector3.Distance(directions[0], Vector3.forward), Is.LessThan(0.0001f));
        }

        [Test]
        public void SpreadDirections_狙った方向を中心に左右対称に広げる()
        {
            var directions = ArrowView.SpreadDirections(Vector3.forward, 3, 15f);

            Assert.That(directions, Has.Length.EqualTo(3));
            Assert.That(Vector3.SignedAngle(Vector3.forward, directions[0], Vector3.up), Is.EqualTo(-15f).Within(0.01f));
            Assert.That(Vector3.SignedAngle(Vector3.forward, directions[1], Vector3.up), Is.EqualTo(0f).Within(0.01f));
            Assert.That(Vector3.SignedAngle(Vector3.forward, directions[2], Vector3.up), Is.EqualTo(15f).Within(0.01f));
        }

        [Test]
        public void TryBounce_壁を越えたら折り返して向きを反転する()
        {
            var bounced = ArrowView.TryBounce(new Vector3(9.5f, 0.85f, 1f), new Vector3(1f, 0f, 1f).normalized, 9f, 6f, out var position, out var direction);

            Assert.That(bounced, Is.True);
            Assert.That(position.x, Is.EqualTo(8.5f).Within(0.0001f));
            Assert.That(position.z, Is.EqualTo(1f));
            Assert.That(direction.x, Is.LessThan(0f));
            Assert.That(direction.z, Is.GreaterThan(0f));
        }

        [Test]
        public void TryBounce_角では両方の向きを反転する()
        {
            var bounced = ArrowView.TryBounce(new Vector3(9.2f, 0f, -6.3f), new Vector3(1f, 0f, -1f).normalized, 9f, 6f, out _, out var direction);

            Assert.That(bounced, Is.True);
            Assert.That(direction.x, Is.LessThan(0f));
            Assert.That(direction.z, Is.GreaterThan(0f));
        }

        [Test]
        public void TryBounce_アリーナの内側や壁から離れる向きなら跳ね返さない()
        {
            Assert.That(ArrowView.TryBounce(new Vector3(8f, 0f, 0f), Vector3.right, 9f, 6f, out _, out _), Is.False);
            Assert.That(ArrowView.TryBounce(new Vector3(9.5f, 0f, 0f), Vector3.left, 9f, 6f, out _, out _), Is.False);
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
