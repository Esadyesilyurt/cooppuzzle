using CoopPuzzle.Gameplay.Core;
using CoopPuzzle.Gameplay.Player;
using Unity.Netcode;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Map
{
    /// <summary>
    /// Bitiş alanı: ilk giren takımın Gezgini maçı kazanır (sunucu otoriteli).
    /// NavMesh gezginleri için sunucuda overlap taraması kullanır (OnTriggerEnter yedek).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class GameplayFinishZone : MonoBehaviour
    {
        [SerializeField] private float checkIntervalSeconds = 0.12f;

        private BoxCollider _box;
        private float _nextCheckTime;

        private void Awake() => EnsureColliderSetup();

        private void Reset() => EnsureColliderSetup();

        private void EnsureColliderSetup()
        {
            var col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            _box = GetComponent<BoxCollider>();
            if (_box == null)
                _box = gameObject.AddComponent<BoxCollider>();

            _box.isTrigger = true;

            var rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void Update()
        {
            if (GameplayWinController.IsMatchEnded)
                return;

            if (Time.time < _nextCheckTime)
                return;

            _nextCheckTime = Time.time + checkIntervalSeconds;

            if (!ShouldRunDetection())
                return;

            if (TryDetectTravelerInZone(out var team))
                GameplayWinController.ReportTeamReachedFinish(team);
        }

        private static bool ShouldRunDetection()
        {
            if (!DoorGameplayNetworkBridge.IsNetworkActive())
                return true;

            var nm = NetworkManager.Singleton;
            return nm != null && nm.IsServer;
        }

        private bool TryDetectTravelerInZone(out SpawnTeam team)
        {
            team = default;

            foreach (var identity in FindObjectsByType<NetworkPlayerIdentity>(FindObjectsSortMode.None))
            {
                if (identity == null)
                    continue;

                var netObj = identity.GetComponent<NetworkObject>();
                if (netObj != null && !netObj.IsSpawned)
                    continue;

                if (identity.GetComponent<NetworkTravelerController>() == null)
                    continue;

                if (!IsInsideZone(identity.transform.position))
                    continue;

                team = identity.Team;
                return true;
            }

            if (!DoorGameplayNetworkBridge.IsNetworkActive())
                return TryDetectOfflineTraveler(out team);

            return false;
        }

        private bool TryDetectOfflineTraveler(out SpawnTeam team)
        {
            team = default;

            foreach (var marker in FindObjectsByType<TravelerTeamMarker>(FindObjectsSortMode.None))
            {
                if (marker == null || !IsInsideZone(marker.transform.position))
                    continue;

                team = marker.Team;
                return true;
            }

            return false;
        }

        private bool IsInsideZone(Vector3 worldPoint)
        {
            var col = _box != null ? _box : GetComponent<BoxCollider>();
            if (col == null)
                return Vector3.Distance(worldPoint, transform.position) <= 2f;

            var local = transform.InverseTransformPoint(worldPoint);
            var half = col.size * 0.5f;
            var min = col.center - half;
            var max = col.center + half;

            return local.x >= min.x && local.x <= max.x
                   && local.y >= min.y && local.y <= max.y
                   && local.z >= min.z && local.z <= max.z;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (GameplayWinController.IsMatchEnded || !ShouldRunDetection())
                return;

            var identity = other.GetComponentInParent<NetworkPlayerIdentity>();
            if (identity == null)
                return;

            if (identity.GetComponent<NetworkTravelerController>() == null)
                return;

            GameplayWinController.ReportTeamReachedFinish(identity.Team);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.9f);
            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(col.center, col.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 1.5f);
            }
        }
#endif
    }
}
