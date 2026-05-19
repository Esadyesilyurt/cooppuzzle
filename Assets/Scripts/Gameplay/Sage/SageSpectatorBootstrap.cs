using CoopPuzzle.Gameplay.Core;
using CoopPuzzle.Gameplay.Map;
using CoopPuzzle.Gameplay.Player;
using TopDownCameraFollow = CoopPuzzle.Gameplay.Camera.TopDownCameraFollow;
using Unity.Netcode;
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
            if (!bindOnStart)
                return;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                return;

            BindTeamTraveler();
        }

        public void BindTeamTraveler()
        {
            var traveler = TeamTravelerLookup.FindTransform(team);
            if (traveler == null)
            {
                Debug.LogWarning($"SageSpectatorBootstrap: {team} Gezgini bulunamadı.");
                return;
            }

            if (followCamera != null)
                followCamera.SetTarget(traveler);

            var router = FindAnyObjectByType<GameplayCameraRouter>();
            router?.SetSageCameraTarget(traveler);
        }

        public void SetTeam(SpawnTeam spawnTeam) => team = spawnTeam;
    }
}
