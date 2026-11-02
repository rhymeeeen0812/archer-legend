using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherLegend.Presentation.Surveys
{
    public interface ISurveyView
    {
        event Action<int> ChoiceClicked;
        event Action<string> TextChanged;
        event Action NextClicked;
        event Action BackClicked;
        event Action CloseClicked;

        void Show();
        void Hide();

        /// <summary>質問の前に、アンケートの説明文を表示します。</summary>
        void ShowIntro(string surveyTitle, string description);

        /// <param name="choices">選択式の選択肢。自由記述のときは null</param>
        void ShowQuestion(string surveyTitle, int pageIndex, int pageCount, string question, string hint, IReadOnlyList<string> choices, string text);

        void SetSelectedChoices(IReadOnlyCollection<int> selected);
        void SetNavigation(bool canGoBack, bool canProceed, bool isLastPage);
        void ShowSubmitting();
        void ShowError(string message);

        /// <summary>質問を隠してメッセージだけを表示します（回答完了など）。</summary>
        void ShowMessage(string message);
    }

    /// <summary>
    /// ゲーム内アンケートの回答画面です。ホーム画面の手前に重ねて表示します。
    /// 見た目はプレハブ（Resources/UI/SurveyView）で編集します。
    /// </summary>
    public sealed class SurveyView : MonoBehaviour, ISurveyView
    {
        private static readonly Color SelectedColor = new Color(0.12f, 0.46f, 0.38f, 0.95f);
        private static readonly Color UnselectedColor = new Color(0.20f, 0.22f, 0.26f, 0.95f);

        public event Action<int> ChoiceClicked;
        public event Action<string> TextChanged;
        public event Action NextClicked;
        public event Action BackClicked;
        public event Action CloseClicked;

        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private TextMeshProUGUI hintText;

        [Tooltip("選択肢のボタンを並べる親です。")]
        [SerializeField] private Transform choiceContainer;

        [Tooltip("選択肢ボタンの見本です。非表示にしておき、選択肢の数だけ複製します。")]
        [SerializeField] private Button choiceTemplate;

        [SerializeField] private TMP_InputField freeTextInput;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TextMeshProUGUI nextButtonLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI closeButtonLabel;

        private readonly List<Button> choiceButtons = new List<Button>();
        private readonly List<string> choiceLabels = new List<string>();

        public void Show()
        {
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
        }

        public void ShowIntro(string surveyTitle, string description)
        {
            SetQuestionVisible(false);
            titleText.text = surveyTitle;
            messageText.text = description;
            backButton.gameObject.SetActive(true);
            backButton.interactable = false;
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            nextButtonLabel.text = "回答をはじめる";
            closeButton.interactable = true;
            closeButtonLabel.text = "あとで";
        }

        public void ShowQuestion(string surveyTitle, int pageIndex, int pageCount, string question, string hint, IReadOnlyList<string> choices, string text)
        {
            SetQuestionVisible(true);
            titleText.text = surveyTitle;
            progressText.text = $"{pageIndex + 1} / {pageCount}";
            questionText.text = question;
            hintText.text = hint;
            errorText.text = string.Empty;
            closeButtonLabel.text = "あとで";

            RebuildChoices(choices ?? Array.Empty<string>());
            freeTextInput.gameObject.SetActive(choices == null);

            // 入力中に書き換えるとカーソルが飛ぶので、ページを表示したときだけ反映する
            freeTextInput.SetTextWithoutNotify(text);
        }

        public void SetSelectedChoices(IReadOnlyCollection<int> selected)
        {
            for (var i = 0; i < choiceButtons.Count; i++)
            {
                var isSelected = selected.Contains(i);
                choiceButtons[i].image.color = isSelected ? SelectedColor : UnselectedColor;
                choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = (isSelected ? "● " : "○ ") + choiceLabels[i];
            }
        }

        public void SetNavigation(bool canGoBack, bool canProceed, bool isLastPage)
        {
            backButton.gameObject.SetActive(true);
            nextButton.gameObject.SetActive(true);
            backButton.interactable = canGoBack;
            nextButton.interactable = canProceed;
            nextButtonLabel.text = isLastPage ? "送信する" : "次へ";
            closeButton.interactable = true;
            freeTextInput.interactable = true;
        }

        public void ShowSubmitting()
        {
            errorText.text = "送信中…";
            freeTextInput.interactable = false;
            backButton.interactable = false;
            nextButton.interactable = false;
            closeButton.interactable = false;
        }

        public void ShowError(string message)
        {
            errorText.text = message;
            closeButton.interactable = true;
        }

        public void ShowMessage(string message)
        {
            SetQuestionVisible(false);
            messageText.text = message;
            backButton.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(false);
            closeButton.interactable = true;
            closeButtonLabel.text = "閉じる";
        }

        private void Awake()
        {
            choiceTemplate.gameObject.SetActive(false);

            // 説明文は長くなることがあるので、文字を小さくして枠に収め、それでも入らない分は省略する
            messageText.fontSizeMax = messageText.fontSize;
            messageText.enableAutoSizing = true;
            messageText.overflowMode = TextOverflowModes.Ellipsis;

            freeTextInput.onValueChanged.AddListener(text => TextChanged?.Invoke(text));
            backButton.onClick.AddListener(() => BackClicked?.Invoke());
            nextButton.onClick.AddListener(() => NextClicked?.Invoke());
            closeButton.onClick.AddListener(() => CloseClicked?.Invoke());
            Hide();
        }

        private void RebuildChoices(IReadOnlyList<string> choices)
        {
            foreach (var button in choiceButtons)
            {
                Destroy(button.gameObject);
            }

            choiceButtons.Clear();
            choiceLabels.Clear();

            for (var i = 0; i < choices.Count; i++)
            {
                var index = i;
                var button = Instantiate(choiceTemplate, choiceContainer);
                button.name = $"Choice {i + 1}";
                button.gameObject.SetActive(true);
                button.onClick.AddListener(() => ChoiceClicked?.Invoke(index));
                choiceButtons.Add(button);
                choiceLabels.Add(choices[i]);
            }
        }

        private void SetQuestionVisible(bool visible)
        {
            progressText.gameObject.SetActive(visible);
            questionText.gameObject.SetActive(visible);
            hintText.gameObject.SetActive(visible);
            choiceContainer.gameObject.SetActive(visible);
            errorText.gameObject.SetActive(visible);
            messageText.gameObject.SetActive(!visible);
            if (!visible)
            {
                freeTextInput.gameObject.SetActive(false);
            }
        }
    }
}
