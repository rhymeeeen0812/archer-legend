using ArcherLegend.Application;
using ArcherLegend.Domain;
using ArcherLegend.Presentation.Game;
using NUnit.Framework;
using UnityEngine;

namespace ArcherLegend.Tests
{
    public class StageTests
    {
        [TestCase(1, 1, 1)]
        [TestCase(3, 1, 3)]
        [TestCase(4, 2, 1)]
        [TestCase(7, 3, 1)]
        [TestCase(9, 3, 3)]
        public void StageProgress_通算のウェーブからステージとステージ内のウェーブを求める(int wave, int stage, int waveInStage)
        {
            Assert.That(StageProgress.StageOf(wave), Is.EqualTo(stage));
            Assert.That(StageProgress.WaveInStage(wave), Is.EqualTo(waveInStage));
        }

        [Test]
        public void StageProgress_ステージの最後のウェーブだけ出口が開く()
        {
            Assert.That(StageProgress.IsLastWaveOfStage(StageProgress.WavesPerStage), Is.True);
            Assert.That(StageProgress.IsLastWaveOfStage(StageProgress.WavesPerStage * 2), Is.True);
            Assert.That(StageProgress.IsLastWaveOfStage(1), Is.False);
            Assert.That(StageProgress.IsLastWaveOfStage(StageProgress.WavesPerStage + 1), Is.False);
        }

        [Test]
        public void Session_ウェーブを進めるとステージも進む()
        {
            using (var session = new ArcherGameSession())
            {
                Assert.That(session.Stage, Is.EqualTo(1));
                Assert.That(session.IsLastWaveOfStage, Is.False);

                for (var i = 1; i < StageProgress.WavesPerStage; i++)
                {
                    session.RegisterWaveCleared();
                }

                Assert.That(session.IsLastWaveOfStage, Is.True);

                session.RegisterWaveCleared();
                Assert.That(session.Stage, Is.EqualTo(2));
                Assert.That(session.WaveInStage, Is.EqualTo(1));

                // サーバーへ送る記録は今までどおり通算のウェーブ数
                Assert.That(session.CreateRunResult().Wave, Is.EqualTo(StageProgress.WavesPerStage + 1));
            }
        }

        [Test]
        public void StageLayouts_最初のステージは障害物なし()
        {
            Assert.That(StageLayouts.For(1), Is.Empty);
        }

        [Test]
        public void StageLayouts_どの配置も入口と出口の周りを空けてアリーナの中に収まる()
        {
            var halfWidth = StageLayouts.ArenaHalfWidth;
            var halfDepth = StageLayouts.ArenaHalfDepth;

            for (var stage = 2; stage < 2 + StageLayouts.PatternCount; stage++)
            {
                var layout = StageLayouts.For(stage);
                Assert.That(layout, Is.Not.Empty, $"ステージ {stage}");

                foreach (var rect in layout)
                {
                    Assert.That(rect.xMin, Is.GreaterThan(-halfWidth + 1f), $"ステージ {stage}");
                    Assert.That(rect.xMax, Is.LessThan(halfWidth - 1f), $"ステージ {stage}");
                    Assert.That(rect.yMin, Is.GreaterThan(-halfDepth + 1f), $"ステージ {stage}");
                    Assert.That(rect.yMax, Is.LessThan(halfDepth - 1f), $"ステージ {stage}");
                    Assert.That(rect.Overlaps(StageLayouts.ExitZone), Is.False, $"ステージ {stage} の出口");

                    var start = StageLayouts.PlayerStart;
                    var pushed = ArenaGeometry.PushOut(start, 1.5f, new[] { rect });
                    Assert.That(pushed, Is.EqualTo(start), $"ステージ {stage} の入口");
                }
            }
        }

        [Test]
        public void StageLayouts_用意した配置を使い切ったら最初の配置に戻る()
        {
            Assert.That(StageLayouts.For(2 + StageLayouts.PatternCount), Is.SameAs(StageLayouts.For(2)));
        }

        [Test]
        public void StageLayouts_プレイヤーが届く位置に出口がある()
        {
            // プレイヤーは壁から 0.6 内側までしか動けない
            Assert.That(StageLayouts.IsInExit(new Vector3(0f, 0.6f, StageLayouts.ArenaHalfDepth - 0.6f)), Is.True);
            Assert.That(StageLayouts.IsInExit(new Vector3(3f, 0.6f, StageLayouts.ArenaHalfDepth - 0.6f)), Is.False);
            Assert.That(StageLayouts.IsInExit(StageLayouts.PlayerStart), Is.False);
        }

        [Test]
        public void PushOut_離れていればそのまま重なっていれば半径の分だけ外へ出す()
        {
            var obstacles = new[] { new Rect(0f, 0f, 2f, 2f) };

            var far = new Vector3(5f, 0.6f, 1f);
            Assert.That(ArenaGeometry.PushOut(far, 0.5f, obstacles), Is.EqualTo(far));

            var touchingLeft = ArenaGeometry.PushOut(new Vector3(-0.2f, 0.6f, 1f), 0.5f, obstacles);
            Assert.That(touchingLeft.x, Is.EqualTo(-0.5f).Within(0.0001f));
            Assert.That(touchingLeft.z, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(touchingLeft.y, Is.EqualTo(0.6f));
        }

        [Test]
        public void PushOut_中心が中に入ってもいちばん近い辺から外へ出す()
        {
            var obstacles = new[] { new Rect(0f, 0f, 4f, 2f) };

            var pushed = ArenaGeometry.PushOut(new Vector3(2f, 0f, 1.8f), 0.5f, obstacles);

            Assert.That(pushed.x, Is.EqualTo(2f));
            Assert.That(pushed.z, Is.EqualTo(2.5f).Within(0.0001f));
        }

        [Test]
        public void SegmentHits_障害物を横切る線分だけ当たる()
        {
            var rect = new Rect(0f, 0f, 1f, 1f);

            Assert.That(ArenaGeometry.SegmentHits(new Vector3(-1f, 0f, 0.5f), new Vector3(2f, 0f, 0.5f), rect, 0f), Is.True);
            Assert.That(ArenaGeometry.SegmentHits(new Vector3(-1f, 0f, 2f), new Vector3(2f, 0f, 2f), rect, 0f), Is.False);
            Assert.That(ArenaGeometry.SegmentHits(new Vector3(-2f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f), rect, 0f), Is.False);
            Assert.That(ArenaGeometry.SegmentHits(new Vector3(-2f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f), rect, 0.6f), Is.True);
        }

        [Test]
        public void ReflectOff_横からぶつかればXを縦からぶつかればZを反転する()
        {
            var rect = new Rect(0f, 0f, 1f, 1f);
            var direction = new Vector3(1f, 0f, 1f).normalized;

            var fromLeft = ArenaGeometry.ReflectOff(rect, new Vector3(-0.2f, 0f, 0.5f), direction);
            Assert.That(fromLeft.x, Is.LessThan(0f));
            Assert.That(fromLeft.z, Is.GreaterThan(0f));

            var fromBelow = ArenaGeometry.ReflectOff(rect, new Vector3(0.5f, 0f, -0.2f), direction);
            Assert.That(fromBelow.x, Is.GreaterThan(0f));
            Assert.That(fromBelow.z, Is.LessThan(0f));
        }
    }
}
