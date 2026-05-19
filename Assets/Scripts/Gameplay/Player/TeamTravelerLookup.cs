using CoopPuzzle.Gameplay.Map;
using Unity.Netcode;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Player
{
    public static class TeamTravelerLookup
    {
        public static TravelerMovementController FindMovement(SpawnTeam team)
        {
            TravelerMovementController markerFallback = null;

            foreach (var movement in Object.FindObjectsByType<TravelerMovementController>(FindObjectsInactive.Exclude))
            {
                if (movement == null || !movement.gameObject.activeInHierarchy)
                    continue;

                if (movement.GetComponent<NetworkObject>() == null)
                    continue;

                var identity = movement.GetComponent<NetworkPlayerIdentity>();
                if (identity != null)
                {
                    if (identity.Team == team)
                        return movement;

                    continue;
                }

                var marker = movement.GetComponent<TravelerTeamMarker>();
                if (marker != null && marker.Team == team)
                    markerFallback = movement;
            }

            return markerFallback;
        }

        public static Transform FindTransform(SpawnTeam team) => FindMovement(team)?.transform;
    }
}
