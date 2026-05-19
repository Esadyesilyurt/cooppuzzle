using UnityEngine;

namespace CoopPuzzle.Gameplay.Map
{
    /// <summary>
    /// Gezgin başlangıç noktası. Bilge haritada spawn olmaz; kendi takımının Gezgin'ini izler (Phase 4).
    /// </summary>
    public sealed class GameplaySpawnPoint : MonoBehaviour
    {
        [SerializeField] private SpawnTeam team = SpawnTeam.Team1;
        [SerializeField] private bool useBakedSpawnPosition = true;
        [SerializeField] private Vector3 bakedSpawnPosition;

        public SpawnTeam Team => team;

        public Vector3 GetSpawnPosition()
        {
            if (useBakedSpawnPosition && bakedSpawnPosition != Vector3.zero)
                return bakedSpawnPosition;

            return transform.position;
        }

        public Quaternion GetSpawnRotation() => transform.rotation;

        public void Configure(SpawnTeam spawnTeam)
        {
            team = spawnTeam;
            name = $"Spawn_{team}_Traveler";
        }

#if UNITY_EDITOR
        public void BakeSpawnPosition()
        {
            if (GameplaySpawnService.TrySnapNearMarker(transform.position, out var snapped))
            {
                bakedSpawnPosition = snapped;
                useBakedSpawnPosition = true;
                UnityEditor.EditorUtility.SetDirty(this);
                return;
            }

            bakedSpawnPosition = transform.position;
            useBakedSpawnPosition = true;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.LogWarning(
                $"[Spawn] {name}: NavMesh bake başarısız; ham marker konumu kaydedildi. Marker'ı yeşil zemine taşıyıp tekrar bake et.");
        }

        private void OnDrawGizmos()
        {
            var spawnPos = useBakedSpawnPosition && bakedSpawnPosition != Vector3.zero
                ? bakedSpawnPosition
                : transform.position;

            var color = team == SpawnTeam.Team1
                ? new Color(1f, 0.25f, 0.25f, 0.9f)
                : new Color(0.2f, 0.45f, 1f, 0.9f);

            Gizmos.color = color;
            Gizmos.DrawWireSphere(spawnPos, 0.6f);
            Gizmos.DrawLine(spawnPos, spawnPos + transform.forward * 1.2f);

            if (useBakedSpawnPosition && bakedSpawnPosition != Vector3.zero
                && Vector3.Distance(spawnPos, transform.position) > 0.05f)
            {
                Gizmos.color = new Color(1f, 1f, 0.2f, 0.7f);
                Gizmos.DrawLine(transform.position, spawnPos);
            }
        }
#endif
    }
}
