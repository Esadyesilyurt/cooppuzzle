using UnityEngine;
using UnityEngine.AI;

namespace CoopPuzzle.Gameplay.Map
{
    public static class GameplaySpawnService
    {
        private const float VerticalSampleHorizontalRadius = 0.35f;
        private const float MaxHorizontalDriftMeters = 0.75f;
        private const float VerticalSearchStep = 0.25f;
        private const float VerticalSearchRange = 40f;

        public static bool TryGetSpawnPoint(SpawnTeam team, out GameplaySpawnPoint spawnPoint)
        {
            spawnPoint = null;

            foreach (var sp in Object.FindObjectsByType<GameplaySpawnPoint>(FindObjectsInactive.Exclude))
            {
                if (sp == null || sp.Team != team)
                    continue;

                spawnPoint = sp;
                return true;
            }

            return false;
        }

        public static bool TryGetSpawnPosition(SpawnTeam team, out Vector3 position) =>
            TryGetSpawnTransform(team, out position, out _);

        public static bool TryGetSpawnTransform(SpawnTeam team, out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.identity;

            if (!TryGetSpawnPoint(team, out var spawn))
                return false;

            rotation = spawn.GetSpawnRotation();
            position = spawn.GetSpawnPosition();
            return true;
        }

        /// <summary>Marker XZ sabit; yalnızca dikey eksende NavMesh arar (kapıya kaymayı önler).</summary>
        public static bool TrySnapNearMarker(Vector3 marker, out Vector3 position)
        {
            position = marker;

            if (!TrySampleVerticalColumn(marker, out var sampled))
                return false;

            if (HorizontalDistance(marker, sampled) > MaxHorizontalDriftMeters)
            {
                Debug.LogWarning(
                    $"[Spawn] NavMesh marker'dan çok uzak ({HorizontalDistance(marker, sampled):F1}m > {MaxHorizontalDriftMeters}m). " +
                    "Marker'ı doğru yere taşı.");
                return false;
            }

            position = sampled;
            return true;
        }

        public static bool IsMarkerOnNavMesh(Vector3 marker, float radius = 0.35f) =>
            NavMesh.SamplePosition(marker, out _, radius, NavMesh.AllAreas);

        public static bool TryFindAnyWalkablePosition(out Vector3 position)
        {
            position = default;
            var triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices == null || triangulation.vertices.Length == 0)
                return false;

            for (var i = 0; i < triangulation.vertices.Length; i += 8)
            {
                var v = triangulation.vertices[i];
                if (NavMesh.SamplePosition(v, out var hit, 4f, NavMesh.AllAreas))
                {
                    position = hit.position;
                    return true;
                }
            }

            return false;
        }

        public static bool TryFindNearestWalkablePosition(Vector3 near, out Vector3 position, float maxRadius = 8f)
        {
            position = near;

            if (NavMesh.SamplePosition(near, out var hit, maxRadius, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }

            return false;
        }

        public static bool TryWarpAgentToNavMesh(NavMeshAgent agent, Vector3 near, float maxSampleRadius = 6f)
        {
            if (agent == null)
                return false;

            if (agent.isOnNavMesh)
                return true;

            if (TrySnapNearMarker(near, out var snapped))
            {
                agent.Warp(snapped);
                return agent.isOnNavMesh;
            }

            if (NavMesh.SamplePosition(near, out var hit, maxSampleRadius, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                return agent.isOnNavMesh;
            }

            return false;
        }

        private static bool TrySampleVerticalColumn(Vector3 marker, out Vector3 position)
        {
            position = marker;

            if (NavMesh.SamplePosition(marker, out var hit, VerticalSampleHorizontalRadius, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }

            for (var offset = VerticalSearchStep; offset <= VerticalSearchRange; offset += VerticalSearchStep)
            {
                var up = marker + Vector3.up * offset;
                if (NavMesh.SamplePosition(up, out hit, VerticalSampleHorizontalRadius, NavMesh.AllAreas))
                {
                    position = hit.position;
                    return true;
                }

                var down = marker + Vector3.down * offset;
                if (NavMesh.SamplePosition(down, out hit, VerticalSampleHorizontalRadius, NavMesh.AllAreas))
                {
                    position = hit.position;
                    return true;
                }
            }

            return false;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
