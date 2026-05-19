using CoopPuzzle.Gameplay.Core;
using CoopPuzzle.Gameplay.Map;

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

            if (agent == null || !agent.enabled)

                return;



            if (!agent.isOnNavMesh)

                return;



            agent.isStopped = !enabled;

            if (!enabled)

                agent.ResetPath();

        }



        public void StopAndClearPath()

        {

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)

                return;



            agent.ResetPath();

            agent.isStopped = true;

            agent.velocity = Vector3.zero;

        }



        public bool TryMoveTo(Vector3 worldPosition)

        {

            if (GameplayWinController.IsMatchEnded)

                return false;

            if (agent == null || !agent.enabled)

                return false;



            if (!agent.isOnNavMesh && !GameplaySpawnService.TryWarpAgentToNavMesh(agent, transform.position))

                return false;



            if (!NavMesh.SamplePosition(worldPosition, out var hit, 4f, NavMesh.AllAreas))

                return false;



            agent.isStopped = false;

            agent.SetDestination(hit.position);

            return true;

        }

    }

}


