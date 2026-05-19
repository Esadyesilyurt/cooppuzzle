using System.Collections;
using CoopPuzzle.Gameplay.Map;
using CoopPuzzle.Gameplay.UI;
using CoopPuzzle.Lobby;
using Unity.Netcode;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Core
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class GameplayWinController : NetworkBehaviour
    {
        public const float WinnerDisplaySeconds = 3f;

        public static GameplayWinController Instance { get; private set; }
        public static bool IsMatchEnded { get; private set; }

        private bool _winnerDeclared;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public override void OnNetworkSpawn()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[Win] Birden fazla GameplayWinController; ilki kullanılıyor.");
                return;
            }

            Instance = this;
            IsMatchEnded = false;
            _winnerDeclared = false;
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void ReportTeamReachedFinish(SpawnTeam team)
        {
            if (IsMatchEnded)
                return;

            var controller = ResolveInstance();
            if (controller == null)
            {
                Debug.LogWarning("[Win] GameplayWinController bulunamadı; yerel kazanma uygulanıyor.");
                DeclareWinnerLocal(team);
                return;
            }

            if (!DoorGameplayNetworkBridge.IsNetworkActive())
            {
                controller.HandleTeamReachedFinishServer(team);
                return;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                controller.HandleTeamReachedFinishServer(team);
            else
                controller.ReportTeamReachedFinishServerRpc((int)team);
        }

        private static GameplayWinController ResolveInstance()
        {
            if (Instance != null)
                return Instance;

            return FindAnyObjectByType<GameplayWinController>();
        }

        [ServerRpc(RequireOwnership = false)]
        private void ReportTeamReachedFinishServerRpc(int teamIndex, ServerRpcParams rpcParams = default)
        {
            HandleTeamReachedFinishServer((SpawnTeam)teamIndex);
        }

        private void HandleTeamReachedFinishServer(SpawnTeam team)
        {
            if (_winnerDeclared)
                return;

            if (DoorGameplayNetworkBridge.IsNetworkActive()
                && NetworkManager.Singleton != null
                && !NetworkManager.Singleton.IsServer)
                return;

            _winnerDeclared = true;
            IsMatchEnded = true;
            Debug.Log($"[Win] Kazanan takım: {team}");
            AnnounceWinnerClientRpc((int)team);
            StartCoroutine(ServerEndMatchAfterDelay());
        }

        [ClientRpc]
        private void AnnounceWinnerClientRpc(int teamIndex)
        {
            IsMatchEnded = true;
            var team = (SpawnTeam)teamIndex;

            var ui = GameplayWinUI.Instance ?? FindAnyObjectByType<GameplayWinUI>();
            if (ui == null)
            {
                var go = new GameObject("GameplayWinUI");
                ui = go.AddComponent<GameplayWinUI>();
            }

            ui.Show(team);
            LobbyCoordinator.Instance?.MarkMatchEndPending();
        }

        private IEnumerator ServerEndMatchAfterDelay()
        {
            yield return new WaitForSeconds(WinnerDisplaySeconds);

            BeginReturnToMenuClientRpc();

            var coordinator = LobbyCoordinator.Instance;
            if (coordinator != null)
            {
                coordinator.BeginReturnToMenu();
                _ = coordinator.EndMatchReturnToLobbyAsHostAsync();
            }
            else
                Debug.LogError("[Win] LobbyCoordinator yok; ana menüye dönülemedi.");
        }

        [ClientRpc]
        private void BeginReturnToMenuClientRpc()
        {
            LobbyCoordinator.Instance?.BeginReturnToMenu();
        }

        private static void DeclareWinnerLocal(SpawnTeam team)
        {
            if (IsMatchEnded)
                return;

            IsMatchEnded = true;
            Debug.Log($"[Win] Kazanan takım (yerel): {team}");

            var ui = GameplayWinUI.Instance ?? FindAnyObjectByType<GameplayWinUI>();
            if (ui == null)
            {
                var go = new GameObject("GameplayWinUI");
                ui = go.AddComponent<GameplayWinUI>();
            }

            ui.Show(team);
            LobbyCoordinator.Instance?.MarkMatchEndPending();
            ui.StartCoroutine(LocalEndMatchAfterDelay());
        }

        private static IEnumerator LocalEndMatchAfterDelay()
        {
            yield return new WaitForSeconds(WinnerDisplaySeconds);
            var coordinator = LobbyCoordinator.Instance;
            if (coordinator == null)
                yield break;

            coordinator.BeginReturnToMenu();
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                _ = coordinator.EndMatchReturnToLobbyAsHostAsync();
        }
    }
}
