using System.Collections.Generic;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Doors
{
    /// <summary>Sahnedeki kapıları ağ senkronu için sayısal kimlikle eşler.</summary>
    public static class DoorRegistry
    {
        private static readonly List<DoorInteractable> Doors = new();

        public static void Register(DoorInteractable door)
        {
            if (door == null || Doors.Contains(door))
                return;

            Doors.Add(door);
        }

        public static void Unregister(DoorInteractable door)
        {
            if (door != null)
                Doors.Remove(door);
        }

        public static int GetId(DoorInteractable door) => door == null ? -1 : Doors.IndexOf(door);

        public static DoorInteractable Get(int id) =>
            id >= 0 && id < Doors.Count ? Doors[id] : null;

        public static void Clear() => Doors.Clear();
    }
}
