using System;
using UnityEngine;

namespace ArcherLegend.Presentation.Title
{
    public interface ITitleView
    {
        event Action StartClicked;
    }

    /// <summary>
    /// タイトル画面の表示です。入力をイベントで通知するだけで、遷移の判断は Presenter に任せます。
    /// </summary>
    public sealed class TitleView : MonoBehaviour, ITitleView
    {
        public event Action StartClicked;

        private void Awake()
        {
            UiFactory.CreateMenuCamera();
            UiFactory.SetupCanvas(gameObject);

            var title = UiFactory.CreateText(transform, "Title", 46);
            title.text = "Archer Legend";
            UiFactory.PlaceCenter(title.rectTransform, new Vector2(0f, 110f), new Vector2(620f, 72f));

            var message = UiFactory.CreateText(transform, "Message", 20);
            message.text = "Press Start";
            UiFactory.PlaceCenter(message.rectTransform, new Vector2(0f, 34f), new Vector2(620f, 40f));

            var startButton = UiFactory.CreateButton(transform, "Start Button", "Start", UiFactory.PrimaryButtonColor, 22);
            UiFactory.PlaceCenter(startButton.GetComponent<RectTransform>(), new Vector2(0f, -62f), new Vector2(240f, 54f));
            startButton.onClick.AddListener(() => StartClicked?.Invoke());
        }
    }
}
