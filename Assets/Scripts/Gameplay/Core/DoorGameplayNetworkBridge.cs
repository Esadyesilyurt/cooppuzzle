using CoopPuzzle.Gameplay.Doors;
using CoopPuzzle.Gameplay.Map;
using CoopPuzzle.Questions;
using Unity.Netcode;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Core
{
    /// <summary>Kapı/soru olaylarını tüm client'lara yayar (Bilge belgesi için).</summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class DoorGameplayNetworkBridge : NetworkBehaviour
    {
        public static DoorGameplayNetworkBridge Instance { get; private set; }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public override void OnNetworkSpawn()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[DoorNetwork] Birden fazla DoorGameplayNetworkBridge; ilki kullanılıyor.");
                return;
            }

            Instance = this;
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this)
                Instance = null;
        }

        public static bool IsNetworkActive() =>
            NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        public void PublishQuestionStarted(DoorInteractable door, SpawnTeam team)
        {
            if (door == null)
                return;

            if (!IsNetworkActive())
            {
                DoorGameplayEvents.RaiseQuestionStarted(door, door.GetQuestion(), team);
                return;
            }

            if (Instance == null)
            {
                Debug.LogWarning("[DoorNetwork] Bridge yok; yerel event kullanılıyor.");
                DoorGameplayEvents.RaiseQuestionStarted(door, door.GetQuestion(), team);
                return;
            }

            var doorId = DoorRegistry.GetId(door);
            if (doorId < 0)
            {
                Debug.LogWarning("[DoorNetwork] Kapı kayıtlı değil.");
                return;
            }

            if (IsServer)
                BroadcastQuestionStartedClientRpc(doorId, (int)team);
            else
                PublishQuestionStartedServerRpc(doorId, (int)team);
        }

        public void PublishQuestionEnded(DoorInteractable door, SpawnTeam team)
        {
            if (door == null)
                return;

            if (!IsNetworkActive())
            {
                DoorGameplayEvents.RaiseQuestionEnded(door, team);
                return;
            }

            if (Instance == null)
            {
                DoorGameplayEvents.RaiseQuestionEnded(door, team);
                return;
            }

            var doorId = DoorRegistry.GetId(door);
            if (doorId < 0)
                return;

            if (IsServer)
                BroadcastQuestionEndedClientRpc(doorId, (int)team);
            else
                PublishQuestionEndedServerRpc(doorId, (int)team);
        }

        [ServerRpc(RequireOwnership = false)]
        private void PublishQuestionStartedServerRpc(int doorId, int teamIndex, ServerRpcParams rpcParams = default)
        {
            BroadcastQuestionStartedClientRpc(doorId, teamIndex);
        }

        [ClientRpc]
        private void BroadcastQuestionStartedClientRpc(int doorId, int teamIndex)
        {
            var door = DoorRegistry.Get(doorId);
            if (door == null)
                return;

            DoorGameplayEvents.RaiseQuestionStarted(door, door.GetQuestion(), (SpawnTeam)teamIndex);
        }

        [ServerRpc(RequireOwnership = false)]
        private void PublishQuestionEndedServerRpc(int doorId, int teamIndex, ServerRpcParams rpcParams = default)
        {
            BroadcastQuestionEndedClientRpc(doorId, teamIndex);
        }

        [ClientRpc]
        private void BroadcastQuestionEndedClientRpc(int doorId, int teamIndex)
        {
            var door = DoorRegistry.Get(doorId);
            if (door == null)
                return;

            DoorGameplayEvents.RaiseQuestionEnded(door, (SpawnTeam)teamIndex);
        }

        public void RequestOpenDoor(DoorInteractable door)
        {
            if (door == null || door.IsOpen)
                return;

            if (!IsNetworkActive())
            {
                door.Open();
                return;
            }

            if (Instance == null)
            {
                door.Open();
                return;
            }

            var doorId = DoorRegistry.GetId(door);
            if (doorId < 0)
            {
                Debug.LogWarning("[DoorNetwork] Açılacak kapı kayıtlı değil.");
                return;
            }

            if (IsServer)
                BroadcastDoorOpenedClientRpc(doorId);
            else
                RequestOpenDoorServerRpc(doorId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestOpenDoorServerRpc(int doorId, ServerRpcParams rpcParams = default)
        {
            BroadcastDoorOpenedClientRpc(doorId);
        }

        [ClientRpc]
        private void BroadcastDoorOpenedClientRpc(int doorId)
        {
            var door = DoorRegistry.Get(doorId);
            if (door == null)
                return;

            door.Open();
        }
    }
}
