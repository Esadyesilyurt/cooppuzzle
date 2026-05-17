using CoopPuzzle.Gameplay.Map;
using CoopPuzzle.Gameplay.Player;
using TopDownCameraFollow = CoopPuzzle.Gameplay.Camera.TopDownCameraFollow;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Sage
{
    public sealed class SageSpectatorBootstrap : MonoBehaviour
    {
        [SerializeField] private SpawnTeam team = SpawnTeam.Team1;
        [SerializeField] private TopDownCameraFollow followCamera;
        [SerializeField] private bool bindOnStart = true;

        private void Start()
        {
            if (bindOnStart)
                BindTeamTraveler();
        }

        public void BindTeamTraveler()
        {
            var traveler = FindTeamTraveler(team);
            if (traveler == null)
            {
                Debug.LogWarning($"SageSpectatorBootstrap: {team} Gezgini bulunamadı.");
                return;
            }

            if (followCamera != null)
                followCamera.SetTarget(traveler);
        }

        private static Transform FindTeamTraveler(SpawnTeam team)
        {
            foreach (var t in FindObjectsByType<TravelerMovementController>(FindObjectsInactive.Exclude))
            {
                var marker = t.GetComponent<TravelerTeamMarker>();
                if (marker == null || marker.Team == team)
                    return t.transform;
            }

            return null;
        }

        public void SetTeam(SpawnTeam spawnTeam) => team = spawnTeam;
    }
}
