using System.Collections;
using CoopPuzzle.Gameplay.Core;
using CoopPuzzle.Gameplay.Doors;
using CoopPuzzle.Gameplay.Map;
using CoopPuzzle.Gameplay.Player;
using CoopPuzzle.Gameplay.UI;
using CoopPuzzle.Questions;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Questions
{
    public sealed class QuestionFlowController : MonoBehaviour
    {
        [SerializeField] private GameplayQuestionUI questionUI;
        [SerializeField] private float wrongAnswerPenaltySeconds = 3f;
        [SerializeField] private float blockWorldInputAfterAnswerSeconds = 0.35f;

        private DoorInteractable _activeDoor;
        private TravelerMovementController _activeTraveler;
        private float _blockWorldInputUntil;
        private Coroutine _penaltyCoroutine;

        public bool IsQuestionOpen => _activeDoor != null && questionUI != null && questionUI.IsVisible;

        public bool BlocksWorldInput =>
            IsQuestionOpen || Time.unscaledTime < _blockWorldInputUntil;

        public DoorInteractable ActiveDoor => _activeDoor;

        private void Awake()
        {
            if (questionUI == null)
                questionUI = FindAnyObjectByType<GameplayQuestionUI>();

            if (questionUI != null)
                questionUI.Hide();
        }

        public void RequestQuestion(DoorInteractable door, TravelerMovementController traveler)
        {
            if (door == null || door.IsOpen || traveler == null)
                return;

            var data = door.GetQuestion();
            if (data == null || !data.IsValid(out _))
            {
                Debug.LogWarning("Bu kapıya soru atanmamış.");
                return;
            }

            _activeDoor = door;
            _activeTraveler = traveler;
            traveler.SetMovementEnabled(false);

            var team = ResolveTeam(traveler);
            var bridge = DoorGameplayNetworkBridge.Instance;
            if (bridge != null)
                bridge.PublishQuestionStarted(door, team);
            else
                DoorGameplayEvents.RaiseQuestionStarted(door, data, team);

            if (ShouldShowTravelerQuestionUi())
                questionUI?.Show(data, OnAnswerSelected);
        }

        private static SpawnTeam ResolveTeam(TravelerMovementController traveler)
        {
            var marker = traveler.GetComponent<TravelerTeamMarker>();
            if (marker != null)
                return marker.Team;

            var identity = traveler.GetComponent<NetworkPlayerIdentity>();
            if (identity != null)
                return identity.Team;

            var session = GameplaySessionConfig.Instance;
            return session != null ? session.LocalTeam : SpawnTeam.Team1;
        }

        private static bool ShouldShowTravelerQuestionUi()
        {
            var session = GameplaySessionConfig.Instance;
            return session == null || session.LocalRole == GameplayRole.Traveler;
        }

        private void OnAnswerSelected(int optionIndex)
        {
            if (_activeDoor == null)
                return;

            var data = _activeDoor.GetQuestion();
            if (data == null)
            {
                CloseQuestionUi();
                return;
            }

            if (optionIndex == data.CorrectOptionIndex)
            {
                var bridge = DoorGameplayNetworkBridge.Instance;
                if (bridge != null)
                    bridge.RequestOpenDoor(_activeDoor);
                else
                    _activeDoor.Open();

                Debug.Log("Doğru cevap — kapı açıldı.");
            }
            else
            {
                ApplyWrongAnswerPenalty();
                Debug.Log($"Yanlış cevap — {wrongAnswerPenaltySeconds}s hareket yok.");
            }

            var wrongAnswer = optionIndex != data.CorrectOptionIndex;
            CloseQuestionUi(keepMovementDisabled: wrongAnswer);
            _blockWorldInputUntil = Time.unscaledTime + blockWorldInputAfterAnswerSeconds;
        }

        private void ApplyWrongAnswerPenalty()
        {
            if (_activeTraveler == null)
                return;

            if (_penaltyCoroutine != null)
                StopCoroutine(_penaltyCoroutine);

            _penaltyCoroutine = StartCoroutine(WrongAnswerPenaltyRoutine(_activeTraveler));
        }

        private IEnumerator WrongAnswerPenaltyRoutine(TravelerMovementController traveler)
        {
            traveler.SetMovementEnabled(false);
            traveler.StopAndClearPath();

            yield return new WaitForSeconds(wrongAnswerPenaltySeconds);

            if (traveler != null)
                traveler.SetMovementEnabled(true);

            _penaltyCoroutine = null;
        }

        private void CloseQuestionUi(bool keepMovementDisabled = false)
        {
            var endedDoor = _activeDoor;
            var endedTraveler = _activeTraveler;
            questionUI?.Hide();
            _activeDoor = null;
            _activeTraveler = null;

            endedTraveler?.StopAndClearPath();
            if (!keepMovementDisabled)
                endedTraveler?.SetMovementEnabled(true);

            if (endedDoor != null)
            {
                var team = endedTraveler != null ? ResolveTeam(endedTraveler) : SpawnTeam.Team1;
                var bridge = DoorGameplayNetworkBridge.Instance;
                if (bridge != null)
                    bridge.PublishQuestionEnded(endedDoor, team);
                else
                    DoorGameplayEvents.RaiseQuestionEnded(endedDoor, team);
            }
        }
    }
}
