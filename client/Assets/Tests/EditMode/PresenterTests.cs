using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ArcherLegend.Application;
using ArcherLegend.Domain;
using ArcherLegend.Presentation.Game;
using ArcherLegend.Presentation.Home;
using ArcherLegend.Presentation.Result;
using NUnit.Framework;
using static ArcherLegend.Tests.PresenterFactory;
using UnityEngine;
using UnityEngine.TestTools;

namespace ArcherLegend.Tests
{
    public class HomePresenterTests
    {
        [Test]
        public async Task InitializeAsync_保存済みの戦績を表示する()
        {
            var repository = new InMemorySaveDataRepository
            {
                Stored = new SaveData { bestScore = 500, bestWave = 9, totalEnemiesDefeated = 80, playCount = 6 }
            };
            var view = new FakeHomeView();
            var presenter = CreateHomePresenter(view, repository, new FakeGameApiClient(), new FakeSceneNavigator());

            await presenter.InitializeAsync();

            Assert.That(view.State, Is.EqualTo("Records"));
            Assert.That(view.Records, Is.EqualTo((500, 9, 80, 6)));
        }

        [Test]
        public async Task InitializeAsync_読み込みに失敗したらエラーを表示する()
        {
            var repository = new InMemorySaveDataRepository { LoadException = new InvalidOperationException("load failed") };
            var view = new FakeHomeView();
            var presenter = CreateHomePresenter(view, repository, new FakeGameApiClient(), new FakeSceneNavigator());
            LogAssert.Expect(LogType.Exception, new Regex("load failed"));

            await presenter.InitializeAsync();

            Assert.That(view.State, Is.EqualTo("LoadError"));
        }

        [Test]
        public async Task InitializeAsync_破棄後に読み込みが終わってもViewを更新しない()
        {
            var repository = new InMemorySaveDataRepository { PendingLoad = new TaskCompletionSource<SaveData>() };
            var view = new FakeHomeView();
            var presenter = CreateHomePresenter(view, repository, new FakeGameApiClient(), new FakeSceneNavigator());
            var task = presenter.InitializeAsync();

            presenter.Dispose();
            repository.PendingLoad.SetResult(new SaveData { bestScore = 1 });
            await task;

            Assert.That(view.State, Is.EqualTo("Loading"));
        }

        [Test]
        public async Task InitializeAsync_戦績の後にランキングを表示する()
        {
            var api = new FakeGameApiClient
            {
                Ranking = new Ranking(new[] { new RankingEntry(1, "トップ", 900, 12, false) }, null)
            };
            var view = new FakeHomeView();
            var presenter = CreateHomePresenter(view, new InMemorySaveDataRepository(), api, new FakeSceneNavigator());

            await presenter.InitializeAsync();

            Assert.That(view.State, Is.EqualTo("Records"));
            Assert.That(view.RankingState, Is.EqualTo("Ranking"));
            Assert.That(view.Ranking, Is.SameAs(api.Ranking));
        }

        [Test]
        public async Task InitializeAsync_サーバーに接続できなくても戦績は表示する()
        {
            var api = new FakeGameApiClient { RegisterException = new GameApiException(0, "offline") };
            var view = new FakeHomeView();
            var presenter = CreateHomePresenter(view, new InMemorySaveDataRepository(), api, new FakeSceneNavigator());

            await presenter.InitializeAsync();

            Assert.That(view.State, Is.EqualTo("Records"));
            Assert.That(view.RankingState, Is.EqualTo("Unavailable"));
        }

        [Test]
        public void ボタン操作で各画面へ遷移する()
        {
            var view = new FakeHomeView();
            var navigator = new FakeSceneNavigator();
            using (CreateHomePresenter(view, new InMemorySaveDataRepository(), new FakeGameApiClient(), navigator))
            {
                view.ClickPlay();
                Assert.That(navigator.Last, Is.EqualTo("Game"));

                view.ClickBackToTitle();
                Assert.That(navigator.Last, Is.EqualTo("Title"));
            }
        }
    }

    public class ResultPresenterTests
    {
        [Test]
        public async Task ShowAsync_保存した結果をサーバーへ送る()
        {
            var repository = new InMemorySaveDataRepository();
            var api = new FakeGameApiClient();
            var presenter = CreateResultPresenter(new FakeResultView(), repository, api, new FakeSceneNavigator(), () => { });

            await presenter.ShowAsync(new RunResult(score: 150, wave: 4, enemiesDefeated: 10, level: 3));

            Assert.That(api.SubmittedRunIds, Has.Count.EqualTo(1));
            Assert.That(repository.Stored.pendingRuns, Is.Empty);
        }

        [Test]
        public async Task ShowAsync_送信に失敗しても結果は送信待ちとして残る()
        {
            var repository = new InMemorySaveDataRepository();
            var api = new FakeGameApiClient { RegisterException = new GameApiException(0, "offline") };
            var view = new FakeResultView();
            var presenter = CreateResultPresenter(view, repository, api, new FakeSceneNavigator(), () => { });

            await presenter.ShowAsync(new RunResult(score: 150, wave: 4, enemiesDefeated: 10, level: 3));

            Assert.That(view.IsVisible, Is.True);
            Assert.That(view.IsSaveErrorShown, Is.False);
            Assert.That(repository.Stored.pendingRuns, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task ShowAsync_記録を保存して自己ベスト更新を表示する()
        {
            var repository = new InMemorySaveDataRepository { Stored = new SaveData { bestScore = 100, bestWave = 5 } };
            var view = new FakeResultView();
            var presenter = CreateResultPresenter(view, repository, new FakeGameApiClient(), new FakeSceneNavigator(), () => { });

            await presenter.ShowAsync(new RunResult(score: 150, wave: 4, enemiesDefeated: 10, level: 3));

            Assert.That(view.IsVisible, Is.True);
            Assert.That(view.Score, Is.EqualTo(150));
            Assert.That(view.IsNewBestScore, Is.True);
            Assert.That(view.IsNewBestWave, Is.False);
            Assert.That(view.IsSaveErrorShown, Is.False);
            Assert.That(repository.Stored.bestScore, Is.EqualTo(150));
        }

        [Test]
        public async Task ShowAsync_保存に失敗しても結果とエラーを表示する()
        {
            var repository = new InMemorySaveDataRepository { SaveException = new InvalidOperationException("save failed") };
            var view = new FakeResultView();
            var presenter = CreateResultPresenter(view, repository, new FakeGameApiClient(), new FakeSceneNavigator(), () => { });
            LogAssert.Expect(LogType.Exception, new Regex("save failed"));

            await presenter.ShowAsync(new RunResult(score: 80, wave: 2, enemiesDefeated: 4, level: 1));

            Assert.That(view.IsVisible, Is.True);
            Assert.That(view.Score, Is.EqualTo(80));
            Assert.That(view.IsSaveErrorShown, Is.True);
        }

        [Test]
        public void Retryで画面を閉じてリトライ処理を呼ぶ()
        {
            var view = new FakeResultView();
            var retried = false;
            using (CreateResultPresenter(view, new InMemorySaveDataRepository(), new FakeGameApiClient(), new FakeSceneNavigator(), () => retried = true))
            {
                view.Show(0, 1, 0, 1, false, false);

                view.ClickRetry();

                Assert.That(retried, Is.True);
                Assert.That(view.IsVisible, Is.False);
            }
        }

        [Test]
        public void Homeでホーム画面へ遷移する()
        {
            var view = new FakeResultView();
            var navigator = new FakeSceneNavigator();
            using (CreateResultPresenter(view, new InMemorySaveDataRepository(), new FakeGameApiClient(), navigator, () => { }))
            {
                view.ClickHome();

                Assert.That(navigator.Last, Is.EqualTo("Home"));
            }
        }
    }

    public class GameHudPresenterTests
    {
        [Test]
        public void Refresh_レベルアップ待ちになったときだけ強化の選択肢を表示する()
        {
            using (var session = new ArcherGameSession())
            {
                var view = new FakeGameHudView();
                var presenter = new GameHudPresenter(view, session);

                presenter.Refresh(aliveEnemies: 0);
                Assert.That(view.IsUpgradesVisible, Is.False);

                LevelUp(session);
                presenter.Refresh(aliveEnemies: 0);
                presenter.Refresh(aliveEnemies: 0);

                Assert.That(view.IsUpgradesVisible, Is.True);
                Assert.That(view.ShowUpgradesCount, Is.EqualTo(1), "毎フレーム作り直さない");
            }
        }

        [Test]
        public void 選択した強化をプレイヤーに適用して選択肢を閉じる()
        {
            using (var session = new ArcherGameSession())
            {
                var view = new FakeGameHudView();
                var presenter = new GameHudPresenter(view, session);
                LevelUp(session);
                presenter.Refresh(aliveEnemies: 0);

                view.SelectUpgrade(0);
                presenter.Refresh(aliveEnemies: 0);

                Assert.That(session.IsUpgradePending, Is.False);
                Assert.That(view.IsUpgradesVisible, Is.False);
            }
        }

        [Test]
        public void 続けてレベルアップしたら新しい選択肢で表示し直す()
        {
            using (var session = new ArcherGameSession())
            {
                var view = new FakeGameHudView();
                var presenter = new GameHudPresenter(view, session);
                LevelUp(session);
                presenter.Refresh(aliveEnemies: 0);
                view.SelectUpgrade(0);

                // 選択直後の同じフレームで次のレベルアップが起きても、古い選択肢を出し続けない
                LevelUp(session);
                presenter.Refresh(aliveEnemies: 0);

                Assert.That(view.ShowUpgradesCount, Is.EqualTo(2));
            }
        }

        [Test]
        public void 範囲外の選択は無視する()
        {
            using (var session = new ArcherGameSession())
            {
                var view = new FakeGameHudView();
                var presenter = new GameHudPresenter(view, session);
                LevelUp(session);

                view.SelectUpgrade(5);

                Assert.That(session.IsUpgradePending, Is.True);
            }
        }

        private static void LevelUp(ArcherGameSession session)
        {
            while (!session.IsUpgradePending)
            {
                session.RegisterEnemyDefeated(session.CreateEnemy());
            }
        }
    }

    internal static class PresenterFactory
    {
        public static HomePresenter CreateHomePresenter(FakeHomeView view, InMemorySaveDataRepository repository, FakeGameApiClient api, FakeSceneNavigator navigator)
        {
            var progress = new PlayerProgressService(repository);
            return new HomePresenter(view, progress, new OnlineSyncService(progress, api), navigator);
        }

        public static ResultPresenter CreateResultPresenter(FakeResultView view, InMemorySaveDataRepository repository, FakeGameApiClient api, FakeSceneNavigator navigator, Action retry)
        {
            var progress = new PlayerProgressService(repository);
            return new ResultPresenter(view, progress, new OnlineSyncService(progress, api), navigator, retry);
        }
    }
}
