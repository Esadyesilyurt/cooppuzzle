using System;
using CoopPuzzle.Gameplay.Core;
using CoopPuzzle.Questions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoopPuzzle.Gameplay.UI
{
  public sealed class GameplayQuestionUI : MonoBehaviour
  {
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private Button[] answerButtons;

    private Action<int> _onAnswer;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
      Hide();
      WireButtons();
    }

    private void WireButtons()
    {
      if (answerButtons == null) return;

      for (int i = 0; i < answerButtons.Length; i++)
      {
        var index = i;
        var btn = answerButtons[i];
        if (btn == null) continue;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => _onAnswer?.Invoke(index));
      }
    }

    public void Show(QuestionData data, Action<int> onAnswer)
    {
      if (data == null) return;

      var session = GameplaySessionConfig.Instance;
      if (session != null && session.LocalRole != GameplayRole.Traveler)
        return;

      _onAnswer = onAnswer;

      if (questionText != null)
        questionText.text = data.QuestionText;

      var options = data.Options;
      for (int i = 0; i < answerButtons.Length; i++)
      {
        var btn = answerButtons[i];
        if (btn == null) continue;

        bool hasOption = options != null && i < options.Count;
        btn.gameObject.SetActive(hasOption);
        if (!hasOption) continue;

        var label = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
          label.text = options[i];

        btn.interactable = true;
      }

      if (panelRoot != null)
        panelRoot.SetActive(true);
    }

    public void Hide()
    {
      _onAnswer = null;
      if (panelRoot != null)
        panelRoot.SetActive(false);
    }
  }
}
