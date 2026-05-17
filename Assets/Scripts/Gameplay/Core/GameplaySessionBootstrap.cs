using CoopPuzzle.Gameplay.Map;
using CoopPuzzle.Lobby;
using Unity.Services.Authentication;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Core
{
    /// <summary>
    /// Oyun sahnesi yüklendiğinde lobby slotundan yerel rol/takım atar.
    /// </summary>
    public sealed class GameplaySessionBootstrap : MonoBehaviour
    {
        [SerializeField] private GameplaySessionConfig sessionConfig;
        [SerializeField] private GameplayCameraRouter cameraRouter;
        [SerializeField] private bool applyOnStart = true;

        private void Start()
        {
            if (!applyOnStart) return;

            if (sessionConfig == null)
                sessionConfig = GameplaySessionConfig.Instance;

            if (cameraRouter == null)
                cameraRouter = FindAnyObjectByType<GameplayCameraRouter>();

            LobbyRoleAssignment.ApplyLocalRoleFromLobby(sessionConfig);
            cameraRouter?.ApplyRole();
        }
    }
}
