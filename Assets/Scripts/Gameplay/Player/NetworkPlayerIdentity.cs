using CoopPuzzle.Gameplay.Map;
using Unity.Netcode;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Player
{
    public sealed class NetworkPlayerIdentity : NetworkBehaviour
    {
        private readonly NetworkVariable<int> _teamIndex = new(
            (int)SpawnTeam.Team1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public SpawnTeam Team => (SpawnTeam)_teamIndex.Value;

        public void SetTeamServer(SpawnTeam team)
        {
            if (!IsServer)
                return;

            _teamIndex.Value = (int)team;
            ApplyTeamMarker();
        }

        public override void OnNetworkSpawn()
        {
            ApplyTeamMarker();
            _teamIndex.OnValueChanged += OnTeamChanged;
        }

        public override void OnNetworkDespawn()
        {
            _teamIndex.OnValueChanged -= OnTeamChanged;
        }

        private void OnTeamChanged(int _, int __) => ApplyTeamMarker();

        private void ApplyTeamMarker()
        {
            var marker = GetComponent<TravelerTeamMarker>();
            marker?.SetTeam(Team);
        }
    }
}
