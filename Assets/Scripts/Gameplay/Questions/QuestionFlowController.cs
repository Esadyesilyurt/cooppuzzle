using CoopPuzzle.Gameplay.Core;
using CoopPuzzle.Gameplay.Doors;
using CoopPuzzle.Gameplay.Player;
using CoopPuzzle.Gameplay.UI;
using CoopPuzzle.Questions;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Questions
{
  public sealed class QuestionFlowController : MonoBehaviour
  {
    [SerializeField] private GameplayQuestionUI questionUI;
    [SerializeField] private TravelerMovementController travelerMovement;
    [SerializeField] private float wrongAnswerPenaltySeconds = 3f;
    [SerializeField] private float blockWorldInputAfterAnswerSeconds = 0.35f;

    private DoorInteractable _activeDoor;
    private float _blockWorldInputUntil;

    public bool IsQuestionOpen => _activeDoor != null && questionUI != null && questionUI.IsVisible;

    public bool BlocksWorldInput =>
        IsQuestionOpen || Time.unscaledTime < _blockWorldInputUntil;

    public DoorInteractable ActiveDoor => _activeDoor;

    private void Awake()
    {
      if (questionUI == null)
        questionUI = FindAnyObjectByType<GameplayQuestionUI>();

      if (travelerMovement == null)
        travelerMovement = FindAnyObjectByType<TravelerMovementController>();

      if (questionUI != null)
        questionUI.Hide();
    }

    public void RequestQuestion(DoorInteractable door)
    {
      if (door == null || door.IsOpen) return;

      var data = door.GetQuestion();
      if (data == null || !data.IsValid(out _))
      {
        Debug.LogWarning("Bu kapıya soru atanmamış.");
        return;
      }

      _activeDoor = door;
      travelerMovement?.SetMovementEnabled(false);
      DoorGameplayEvents.RaiseQuestionStarted(door, data);

      if (ShouldShowTravelerQuestionUi())
        questionUI?.Show(data, OnAnswerSelected);
    }

    private static bool ShouldShowTravelerQuestionUi()
    {
      var session = GameplaySessionConfig.Instance;
      return session == null || session.LocalRole == GameplayRole.Traveler;
    }

    private void OnAnswerSelected(int optionIndex)
    {
      if (_activeDoor == null) return;

      var data = _activeDoor.GetQuestion();
      if (data == null)
      {
        CloseQuestionUi();
        return;
      }

      if (optionIndex == data.CorrectOptionIndex)
      {
        _activeDoor.Open();
        Debug.Log("Doğru cevap — kapı açıldı.");
      }
      else
      {
        Debug.Log($"Yanlış cevap — {wrongAnswerPenaltySeconds}s ceza (stub).");
      }

      CloseQuestionUi();
      _blockWorldInputUntil = Time.unscaledTime + blockWorldInputAfterAnswerSeconds;
    }

    private void CloseQuestionUi()
    {
      var endedDoor = _activeDoor;
      questionUI?.Hide();
      _activeDoor = null;
      travelerMovement?.StopAndClearPath();
      travelerMovement?.SetMovementEnabled(true);

      if (endedDoor != null)
        DoorGameplayEvents.RaiseQuestionEnded(endedDoor);
    }
  }
}
