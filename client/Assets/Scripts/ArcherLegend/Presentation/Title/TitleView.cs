using System;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherLegend.Presentation.Title
{
    public interface ITitleView
    {
        event Action StartClicked;
    }

    /// <summary>
    /// タイトル画面の表示です。入力をイベントで通知するだけで、遷移の判断は Presenter に任せます。
    /// 見た目はプレハブ（Resources/UI/TitleView）で編集します。
    /// </summary>
    public sealed class TitleView : MonoBehaviour, ITitleView
    {
        public event Action StartClicked;

        [SerializeField] private Button startButton;

        private void Awake()
        {
            MenuCamera.Create();
            startButton.onClick.AddListener(() => StartClicked?.Invoke());
        }
    }
}
