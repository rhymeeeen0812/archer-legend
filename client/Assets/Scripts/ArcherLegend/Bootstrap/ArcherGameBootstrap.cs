using ArcherLegend.Application;
using ArcherLegend.Infrastructure;
using ArcherLegend.Presentation;
using ArcherLegend.Presentation.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcherLegend.Bootstrap
{
    /// <summary>
    /// 依存関係の組み立て場所です。シーンごとに必要な表示役と依存関係を生成します。
    /// </summary>
    public static class ArcherGameBootstrap
    {
        private static bool isSubscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!isSubscribed)
            {
                SceneManager.sceneLoaded += (_, __) => ConfigureActiveScene();
                isSubscribed = true;
            }

            ConfigureActiveScene();
        }

        private static void ConfigureActiveScene()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == SceneNames.Title)
            {
                CreateTitleScene();
                return;
            }

            if (sceneName == SceneNames.Home)
            {
                CreateHomeScene();
                return;
            }

            CreateGameScene();
        }

        private static void CreateTitleScene()
        {
            if (Object.FindFirstObjectByType<MenuScenePresenter>() != null)
            {
                return;
            }

            var presenter = new GameObject("Title Scene Presenter").AddComponent<MenuScenePresenter>();
            presenter.ShowTitle();
        }

        private static void CreateHomeScene()
        {
            if (Object.FindFirstObjectByType<MenuScenePresenter>() != null)
            {
                return;
            }

            var presenter = new GameObject("Home Scene Presenter").AddComponent<MenuScenePresenter>();
            presenter.ShowHome();
        }

        private static void CreateGameScene()
        {
            if (Object.FindFirstObjectByType<ArcherGameController>() != null)
            {
                return;
            }

            var root = new GameObject("Archer Legend Root");
            var controller = root.AddComponent<ArcherGameController>();
            controller.Construct(new ArcherGameSession(), new UnityObjectFactory());
        }
    }
}
