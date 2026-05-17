using System;
using CoopPuzzle.Gameplay.Doors;
using CoopPuzzle.Questions;

namespace CoopPuzzle.Gameplay.Core
{
    public static class DoorGameplayEvents
    {
        public static event Action<DoorInteractable, QuestionData> TravelerDoorQuestionStarted;
        public static event Action<DoorInteractable> TravelerDoorQuestionEnded;

        public static void RaiseQuestionStarted(DoorInteractable door, QuestionData data) =>
            TravelerDoorQuestionStarted?.Invoke(door, data);

        public static void RaiseQuestionEnded(DoorInteractable door) =>
            TravelerDoorQuestionEnded?.Invoke(door);
    }
}
