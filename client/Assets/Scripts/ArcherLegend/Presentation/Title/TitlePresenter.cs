using System;

namespace ArcherLegend.Presentation.Title
{
    public sealed class TitlePresenter : IDisposable
    {
        private readonly ITitleView view;
        private readonly ISceneNavigator navigator;

        public TitlePresenter(ITitleView view, ISceneNavigator navigator)
        {
            this.view = view;
            this.navigator = navigator;
            view.StartClicked += OnStartClicked;
        }

        public void Dispose()
        {
            view.StartClicked -= OnStartClicked;
        }

        private void OnStartClicked()
        {
            navigator.GoToHome();
        }
    }
}
