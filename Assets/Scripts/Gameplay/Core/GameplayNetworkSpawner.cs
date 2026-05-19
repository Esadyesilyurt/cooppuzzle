using System.Collections.Generic;
using CoopPuzzle.Core.Bootstrap;
using CoopPuzzle.Gameplay.Map;
using CoopPuzzle.Gameplay.Player;
using CoopPuzzle.Lobby;
using Unity.Netcode;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Core
{
    /// <summary>
    /// Takım başına yalnızca 1 Gezgin (NetworkTraveler) spawn eder.
    /// Bilge oyuncular sahne Traveler prefab'ı almaz; Sage UI ile takım gezginini izler.
    /// </summary>
    public sealed class GameplayNetworkSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject networkTravelerPrefab;

        private readonly HashSet<SpawnTeam> _spawnedTeams = new();

        private void Start()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                return;

            EnsureNetworkServicesSpawned();
            SpawnTeamTravelersFromLobby();
        }

        private void EnsureNetworkServicesSpawned()
        {
            if (GetComponent<DoorGameplayNetworkBridge>() == null)
                gameObject.AddComponent<DoorGameplayNetworkBridge>();

            if (GetComponent<GameplayWinController>() == null)
                gameObject.AddComponent<GameplayWinController>();

            var netObj = GetComponent<NetworkObject>();
            if (netObj == null)
            {
                netObj = gameObject.AddComponent<NetworkObject>();
                if (!netObj.IsSpawned)
                    netObj.Spawn();
            }
        }

        private void SpawnTeamTravelersFromLobby()
        {
            var lobby = LobbyCoordinator.Instance?.LobbyService?.CurrentLobby;
            if (lobby?.Players == null || lobby.Players.Count == 0)
            {
                Debug.LogWarning("[GameplaySpawn] Lobby oyuncu listesi yok.");
                return;
            }

            foreach (var player in lobby.Players)
            {
                if (player == null || string.IsNullOrEmpty(player.Id))
                    continue;

                if (!LobbyRoleAssignment.TryGetRoleForLobbyPlayer(player, out var role)
                    || role != GameplayRole.Traveler)
                    continue;

                if (!LobbyRoleAssignment.TryGetTeamForLobbyPlayer(player, out var team))
                    continue;

                if (_spawnedTeams.Contains(team))
                    continue;

                if (!NetworkConnectionRegistry.TryGetClientId(player.Id, out var clientId))
                {
                    Debug.LogWarning($"[GameplaySpawn] {team} gezgini için NGO client bulunamadı ({player.Id}).");
                    continue;
                }

                if (SpawnTravelerForTeam(team, clientId))
                    _spawnedTeams.Add(team);
            }
        }

        private bool SpawnTravelerForTeam(SpawnTeam team, ulong ownerClientId)
        {
            if (networkTravelerPrefab == null)
            {
                Debug.LogError("[GameplaySpawn] networkTravelerPrefab atanmadı. Phase 6 Setup çalıştır.");
                return false;
            }

            GameplaySpawnPoint spawnMarker = null;
            if (!GameplaySpawnService.TryGetSpawnTransform(team, out var position, out var rotation))
            {
                Debug.LogWarning($"[GameplaySpawn] {team} spawn noktası yok; (0,1,0) kullanılıyor.");
                position = new Vector3(0f, 1f, 0f);
                rotation = Quaternion.identity;
            }
            else
            {
                GameplaySpawnService.TryGetSpawnPoint(team, out spawnMarker);
            }

            var instance = Instantiate(networkTravelerPrefab, position, rotation);

            var teamMarker = instance.GetComponent<TravelerTeamMarker>();
            teamMarker?.SetTeam(team);
            var netObj = instance.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Destroy(instance);
                Debug.LogError("[GameplaySpawn] Prefab'da NetworkObject yok.");
                return false;
            }

            netObj.SpawnAsPlayerObject(ownerClientId, destroyWithScene: true);

            var identity = instance.GetComponent<NetworkPlayerIdentity>();
            identity?.SetTeamServer(team);

            var agent = instance.GetComponent<UnityEngine.AI.NavMeshAgent>();
            var warpAt = spawnMarker != null ? spawnMarker.GetSpawnPosition() : position;
            var warped = GameplaySpawnService.TryWarpAgentToNavMesh(agent, warpAt, maxSampleRadius: 0.35f);

            Debug.Log(
                $"[GameplaySpawn] {team} gezgini → client {ownerClientId} @ {instance.transform.position} " +
                $"(hedef:{warpAt}, nav:{warped})");
            return true;
        }

    }
}
