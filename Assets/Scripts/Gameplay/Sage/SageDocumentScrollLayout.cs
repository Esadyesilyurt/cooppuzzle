using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoopPuzzle.Gameplay.Sage
{
    /// <summary>
    /// Uzun belge metninde ScrollRect içeriğinin yüksekliğini TMP'ye göre ayarlar.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public sealed class SageDocumentScrollLayout : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private float horizontalPadding = 32f;
        [SerializeField] private float verticalPadding = 32f;
        [SerializeField] private float minContentHeight = 200f;

        private ScrollRect _scrollRect;

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
            if (bodyText == null && _scrollRect != null && _scrollRect.content != null)
                bodyText = _scrollRect.content.GetComponentInChildren<TextMeshProUGUI>(true);

            ConfigureScrollRect();
            ConfigureBodyAnchors();
        }

        public void RefreshContentSize()
        {
            if (_scrollRect == null || bodyText == null || _scrollRect.content == null)
                return;

            ConfigureBodyAnchors();

            var viewportWidth = _scrollRect.viewport != null
                ? _scrollRect.viewport.rect.width
                : bodyText.rectTransform.rect.width;

            var textWidth = Mathf.Max(100f, viewportWidth - horizontalPadding);
            bodyText.enableWordWrapping = true;
            bodyText.overflowMode = TextOverflowModes.Overflow;
            bodyText.ForceMeshUpdate(true, true);

            var preferred = bodyText.GetPreferredValues(textWidth, 0f);
            var bodyHeight = Mathf.Max(preferred.y, bodyText.fontSize);
            var contentHeight = Mathf.Max(minContentHeight, bodyHeight + verticalPadding);

            var bodyRect = bodyText.rectTransform;
            bodyRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth);
            bodyRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bodyHeight);

            var content = _scrollRect.content;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Canvas.ForceUpdateCanvases();

            _scrollRect.verticalNormalizedPosition = 1f;
        }

        private void ConfigureScrollRect()
        {
            if (_scrollRect == null) return;

            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 40f;
            _scrollRect.inertia = true;
        }

        private void ConfigureBodyAnchors()
        {
            if (bodyText == null || _scrollRect?.content == null) return;

            var bodyRect = bodyText.rectTransform;
            bodyRect.SetParent(_scrollRect.content, false);
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = new Vector2(0f, -verticalPadding * 0.5f);
        }
    }
}
