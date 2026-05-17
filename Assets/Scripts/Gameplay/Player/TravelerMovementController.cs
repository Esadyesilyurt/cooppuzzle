using UnityEngine;
using UnityEngine.AI;

namespace CoopPuzzle.Gameplay.Player
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class TravelerMovementController : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private float stoppingDistance = 0.15f;

        public bool IsMoving => agent != null && agent.enabled && !agent.isStopped &&
                                agent.remainingDistance > stoppingDistance;

        private void Reset()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Awake()
        {
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();

            agent.stoppingDistance = stoppingDistance;
        }

        public void SetMovementEnabled(bool enabled)
        {
            if (agent == null) return;
            agent.isStopped = !enabled;
            if (!enabled)
                agent.ResetPath();
        }

        public void StopAndClearPath()
        {
            if (agent == null) return;
            agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        public bool TryMoveTo(Vector3 worldPosition)
        {
            if (agent == null || !agent.enabled)
                return false;

            if (!NavMesh.SamplePosition(worldPosition, out var hit, 2f, NavMesh.AllAreas))
                return false;

            agent.isStopped = false;
            agent.SetDestination(hit.position);
            return true;
        }
    }
}
