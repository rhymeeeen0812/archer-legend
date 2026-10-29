using ArcherLegend.Application;
using ArcherLegend.Infrastructure;
using ArcherLegend.Presentation;
using ArcherLegend.Presentation.Game;
using ArcherLegend.Presentation.Home;
using ArcherLegend.Presentation.Title;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcherLegend.Bootstrap
{
    /// <summary>
    /// 依存関係の組み立て場所です。アプリ全体で共有するサービスを 1 度だけ作り、シーンごとに View と Presenter を結び付けます。
    /// </summary>
    public static class ArcherGameBootstrap
    {
        private static bool isSubscribed;
        private static PlayerProgressService progress;
        private static ISceneNavigator navigator;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            // ArcherLegend.Application 名前空間と衝突するため、UnityEngine を明示する
            progress ??= new PlayerProgressService(new JsonFileSaveDataRepository(UnityEngine.Application.persistentDataPath));
            navigator ??= new SceneNavigator();

            if (!isSubscribed)
            {
                SceneManager.sceneLoaded += (_, __) => ConfigureActiveScene();
                isSubscribed = true;
            }

            ConfigureActiveScene();
        }

        private static void ConfigureActiveScene()
        {
            switch (SceneManager.GetActiveScene().name)
            {
                case SceneNames.Title:
                    CreateTitleScene();
                    break;
                case SceneNames.Home:
                    CreateHomeScene();
                    break;
                case SceneNames.Game:
                    CreateGameScene();
                    break;
            }
        }

        private static void CreateTitleScene()
        {
            if (Object.FindFirstObjectByType<TitleView>() != null)
            {
                return;
            }

            var view = new GameObject("Title View").AddComponent<TitleView>();
            PresenterLifetime.Bind(view.gameObject, new TitlePresenter(view, navigator));
        }

        private static void CreateHomeScene()
        {
            if (Object.FindFirstObjectByType<HomeView>() != null)
            {
                return;
            }

            var view = new GameObject("Home View").AddComponent<HomeView>();
            var presenter = new HomePresenter(view, progress, navigator);
            PresenterLifetime.Bind(view.gameObject, presenter);
            _ = presenter.InitializeAsync();
        }

        private static void CreateGameScene()
        {
            if (Object.FindFirstObjectByType<ArcherGameController>() != null)
            {
                return;
            }

            var root = new GameObject("Archer Legend Root");
            var controller = root.AddComponent<ArcherGameController>();
            controller.Construct(new ArcherGameSession(), new UnityObjectFactory(), progress, navigator);
        }
    }
}
