using UnityEngine.SceneManagement;

namespace ArcherLegend.Presentation
{
    /// <summary>
    /// 画面遷移の窓口です。Presenter が SceneManager に直接依存しないようにして、テストで差し替えられるようにします。
    /// </summary>
    public interface ISceneNavigator
    {
        void GoToTitle();
        void GoToHome();
        void GoToGame();
    }

    public sealed class SceneNavigator : ISceneNavigator
    {
        public void GoToTitle() => SceneManager.LoadScene(SceneNames.Title);
        public void GoToHome() => SceneManager.LoadScene(SceneNames.Home);
        public void GoToGame() => SceneManager.LoadScene(SceneNames.Game);
    }
}
