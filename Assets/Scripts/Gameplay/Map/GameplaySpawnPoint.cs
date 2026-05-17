using UnityEngine;

namespace CoopPuzzle.Gameplay.Map
{
    /// <summary>
    /// Gezgin başlangıç noktası. Bilge haritada spawn olmaz; kendi takımının Gezgin'ini izler (Phase 4).
    /// </summary>
    public sealed class GameplaySpawnPoint : MonoBehaviour
    {
        [SerializeField] private SpawnTeam team = SpawnTeam.Team1;

        public SpawnTeam Team => team;

        public void Configure(SpawnTeam spawnTeam)
        {
            team = spawnTeam;
            name = $"Spawn_{team}_Traveler";
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, 0.6f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.2f);
        }
#endif
    }
}
