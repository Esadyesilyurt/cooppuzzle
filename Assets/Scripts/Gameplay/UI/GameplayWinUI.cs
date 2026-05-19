using CoopPuzzle.Gameplay.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoopPuzzle.Gameplay.UI
{
  public sealed class GameplayWinUI : MonoBehaviour
  {
    public static GameplayWinUI Instance { get; private set; }

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI messageText;

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }

      Instance = this;
      EnsureOverlay();
      Hide();
    }

    private void OnDestroy()
    {
      if (Instance == this)
        Instance = null;
    }

    public void Show(SpawnTeam winningTeam)
    {
      EnsureOverlay();
      if (messageText != null)
        messageText.text = $"{GetTeamDisplayName(winningTeam)} takım kazandı!";

      if (panelRoot != null)
        panelRoot.SetActive(true);
    }

    public void Hide()
    {
      if (panelRoot != null)
        panelRoot.SetActive(false);
    }

    public static string GetTeamDisplayName(SpawnTeam team) =>
      team switch
      {
        SpawnTeam.Team1 => "Kırmızı",
        SpawnTeam.Team2 => "Mavi",
        _ => team.ToString()
      };

    private void EnsureOverlay()
    {
      if (panelRoot != null && messageText != null)
        return;

      var canvasGo = new GameObject("WinOverlayCanvas");
      canvasGo.transform.SetParent(transform, false);

      var canvas = canvasGo.AddComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
      canvas.sortingOrder = 500;
      canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      canvasGo.AddComponent<GraphicRaycaster>();

      panelRoot = new GameObject("WinPanel");
      panelRoot.transform.SetParent(canvasGo.transform, false);

      var panelImage = panelRoot.AddComponent<Image>();
      panelImage.color = new Color(0f, 0f, 0f, 0.72f);

      var panelRect = panelRoot.GetComponent<RectTransform>();
      panelRect.anchorMin = Vector2.zero;
      panelRect.anchorMax = Vector2.one;
      panelRect.offsetMin = Vector2.zero;
      panelRect.offsetMax = Vector2.zero;

      var textGo = new GameObject("WinMessage");
      textGo.transform.SetParent(panelRoot.transform, false);

      messageText = textGo.AddComponent<TextMeshProUGUI>();
      messageText.alignment = TextAlignmentOptions.Center;
      messageText.fontSize = 48;
      messageText.color = Color.white;

      var textRect = textGo.GetComponent<RectTransform>();
      textRect.anchorMin = new Vector2(0.1f, 0.35f);
      textRect.anchorMax = new Vector2(0.9f, 0.65f);
      textRect.offsetMin = Vector2.zero;
      textRect.offsetMax = Vector2.zero;
    }
  }
}
