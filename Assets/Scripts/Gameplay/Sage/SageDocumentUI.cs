using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoopPuzzle.Gameplay.Sage
{
    /// <summary>
    /// Bilge tam ekran bilgi belgesi (soru paneli değil).
    /// </summary>
    public sealed class SageDocumentUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Button closeButton;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private SageDocumentScrollLayout scrollLayout;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (scrollLayout == null && scrollRect != null)
                scrollLayout = scrollRect.GetComponent<SageDocumentScrollLayout>();

            Hide();
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
        }

        public void Show(string title, string body, string hint = null)
        {
            if (titleText != null)
                titleText.text = string.IsNullOrWhiteSpace(title) ? "Bilgi Belgesi" : title;

            if (hintText != null)
            {
                var showHint = !string.IsNullOrWhiteSpace(hint);
                hintText.gameObject.SetActive(showHint);
                if (showHint)
                    hintText.text = hint;
            }

            if (bodyText != null)
                bodyText.text = body ?? string.Empty;

            if (panelRoot != null)
                panelRoot.SetActive(true);

            if (scrollLayout != null)
                scrollLayout.RefreshContentSize();
            else if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }
    }
}
