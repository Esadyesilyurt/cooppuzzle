using System.Collections;
using CoopPuzzle.Gameplay.Map;
using CoopPuzzle.Gameplay.Player;
using CoopPuzzle.Gameplay.Sage;
using CoopPuzzle.Lobby;
using Unity.Netcode;
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

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                DisableScenePlaceholders();
            else
            {
                SnapOfflineTravelerToTeamSpawn();
                BindOfflineTravelerCamera(sessionConfig, cameraRouter);
            }

            cameraRouter?.ApplyRole();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                if (sessionConfig.LocalRole == GameplayRole.Traveler)
                    StartCoroutine(BindTravelerCameraWhenReady(sessionConfig, cameraRouter));
                else if (sessionConfig.LocalRole == GameplayRole.Sage)
                    StartCoroutine(BindSageToTeamTravelerWhenReady(sessionConfig.LocalTeam));
            }
        }

        private static IEnumerator BindTravelerCameraWhenReady(
            GameplaySessionConfig session,
            GameplayCameraRouter router)
        {
            if (session == null || session.LocalRole != GameplayRole.Traveler)
                yield break;

            for (var i = 0; i < 180; i++)
            {
                if (LocalPlayerLookup.GetLocalTraveler() != null)
                    break;

                yield return null;
            }

            router ??= FindAnyObjectByType<GameplayCameraRouter>();
            router?.TryBindLocalNetworkTraveler();
        }

        private static IEnumerator BindSageToTeamTravelerWhenReady(SpawnTeam team)
        {
            for (var i = 0; i < 300; i++)
            {
                var movement = TeamTravelerLookup.FindMovement(team);
                if (movement != null)
                {
                    var identity = movement.GetComponent<NetworkPlayerIdentity>();
                    if (identity == null || identity.Team == team)
                        break;
                }

                yield return null;
            }

            ApplySageBindings(team);
            yield return new WaitForSeconds(1.5f);
            ApplySageBindings(team);
        }

        private static void ApplySageBindings(SpawnTeam team)
        {
            foreach (var flow in FindObjectsByType<SageDocumentFlowController>(FindObjectsInactive.Exclude))
                flow.SetWatchTeam(team);

            var router = FindAnyObjectByType<GameplayCameraRouter>();
            var traveler = TeamTravelerLookup.FindTransform(team);
            router?.SetSageCameraTarget(traveler);

            foreach (var sage in FindObjectsByType<SageSpectatorBootstrap>(FindObjectsInactive.Exclude))
            {
                sage.SetTeam(team);
                sage.BindTeamTraveler();
            }
        }

        private static void DisableScenePlaceholders()
        {
            var router = Object.FindAnyObjectByType<GameplayCameraRouter>();
            router?.ClearTravelerCameraTarget();

            foreach (var movement in Object.FindObjectsByType<TravelerMovementController>(FindObjectsInactive.Include))
            {
                if (movement == null || movement.GetComponent<NetworkObject>() != null)
                    continue;

                movement.gameObject.SetActive(false);
            }
        }

        private static void BindOfflineTravelerCamera(GameplaySessionConfig session, GameplayCameraRouter router)
        {
            if (session == null || router == null || session.LocalRole != GameplayRole.Traveler)
                return;

            var traveler = TeamTravelerLookup.FindTransform(session.LocalTeam);
            if (traveler == null)
                return;

            router.SetTravelerCameraTarget(traveler);

            var touch = traveler.GetComponent<TravelerTouchInput>();
            if (touch != null)
                router.BindLocalTraveler(touch);
        }

        private static void SnapOfflineTravelerToTeamSpawn()
        {
            var session = GameplaySessionConfig.Instance;
            if (session == null)
                return;

            TravelerMovementController traveler = null;
            foreach (var t in Object.FindObjectsByType<TravelerMovementController>(FindObjectsInactive.Exclude))
            {
                if (t.GetComponent<NetworkObject>() == null)
                {
                    traveler = t;
                    break;
                }
            }

            if (traveler == null)
                return;

            if (GameplaySpawnService.TryGetSpawnPosition(session.LocalTeam, out var pos))
                traveler.transform.position = pos;
        }
    }
}
