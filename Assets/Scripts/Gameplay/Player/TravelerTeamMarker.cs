using CoopPuzzle.Gameplay.Map;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Player
{
    public sealed class TravelerTeamMarker : MonoBehaviour
    {
        [SerializeField] private SpawnTeam team = SpawnTeam.Team1;

        public SpawnTeam Team => team;

        public void SetTeam(SpawnTeam spawnTeam) => team = spawnTeam;
    }
}
