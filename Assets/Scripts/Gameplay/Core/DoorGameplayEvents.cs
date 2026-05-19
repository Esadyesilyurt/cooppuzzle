using System;
using CoopPuzzle.Gameplay.Doors;
using CoopPuzzle.Gameplay.Map;
using CoopPuzzle.Questions;

namespace CoopPuzzle.Gameplay.Core
{
    public static class DoorGameplayEvents
    {
        public static event Action<DoorInteractable, QuestionData, SpawnTeam> TravelerDoorQuestionStarted;
        public static event Action<DoorInteractable, SpawnTeam> TravelerDoorQuestionEnded;

        public static void RaiseQuestionStarted(DoorInteractable door, QuestionData data, SpawnTeam team) =>
            TravelerDoorQuestionStarted?.Invoke(door, data, team);

        public static void RaiseQuestionEnded(DoorInteractable door, SpawnTeam team) =>
            TravelerDoorQuestionEnded?.Invoke(door, team);
    }
}
