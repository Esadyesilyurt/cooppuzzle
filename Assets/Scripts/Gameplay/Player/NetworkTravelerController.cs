using System.Collections;

using CoopPuzzle.Gameplay.Core;

using CoopPuzzle.Gameplay.Map;

using Unity.Netcode;

using UnityEngine;

using UnityEngine.AI;



namespace CoopPuzzle.Gameplay.Player

{

    /// <summary>NGO sahipliği: yalnızca owner input/agent kullanır; pozisyon NetworkTransform ile senkron.</summary>

    [RequireComponent(typeof(NetworkObject))]

    public sealed class NetworkTravelerController : NetworkBehaviour

    {

        [SerializeField] private TravelerMovementController movement;

        [SerializeField] private TravelerTouchInput touchInput;

        [SerializeField] private NavMeshAgent agent;



        public bool IsLocallyControlled => IsOwner;



        private void Reset()

        {

            movement = GetComponent<TravelerMovementController>();

            touchInput = GetComponent<TravelerTouchInput>();

            agent = GetComponent<NavMeshAgent>();

        }



        private void Awake()

        {

            if (movement == null) movement = GetComponent<TravelerMovementController>();

            if (touchInput == null) touchInput = GetComponent<TravelerTouchInput>();

            if (agent == null) agent = GetComponent<NavMeshAgent>();

        }



        public override void OnNetworkSpawn()

        {

            ApplyOwnership();



            if (IsOwner)

            {

                StartCoroutine(BindCameraWhenReady());

                StartCoroutine(EnsureNavMeshWhenReady());

            }

        }



        private IEnumerator EnsureNavMeshWhenReady()

        {

            var warpNear = transform.position;

            var identity = GetComponent<NetworkPlayerIdentity>();

            if (identity != null && GameplaySpawnService.TryGetSpawnPosition(identity.Team, out var spawnPos))

                warpNear = spawnPos;



            for (var i = 0; i < 30; i++)

            {

                if (agent != null && agent.isOnNavMesh)

                    break;



                if (agent != null)

                    GameplaySpawnService.TryWarpAgentToNavMesh(agent, warpNear, maxSampleRadius: 6f);



                yield return null;

            }

        }



        private IEnumerator BindCameraWhenReady()

        {

            for (var i = 0; i < 120; i++)

            {

                var router = FindAnyObjectByType<GameplayCameraRouter>();

                if (router != null)

                {

                    BindLocalCamera(router);

                    yield break;

                }



                yield return null;

            }



            BindLocalCamera(FindAnyObjectByType<GameplayCameraRouter>());

        }



        private void ApplyOwnership()

        {

            var mine = IsOwner;



            if (agent != null)

            {

                var warpNear = transform.position;

                var identity = GetComponent<NetworkPlayerIdentity>();

                if (identity != null && GameplaySpawnService.TryGetSpawnPosition(identity.Team, out var spawnPos))

                    warpNear = spawnPos;

                GameplaySpawnService.TryWarpAgentToNavMesh(agent, warpNear, maxSampleRadius: 6f);

            }



            if (touchInput != null)

                touchInput.enabled = mine;



            if (agent != null)

                agent.enabled = mine;



            if (mine)

                BindLocalCamera(FindAnyObjectByType<GameplayCameraRouter>());

        }



        private void BindLocalCamera(GameplayCameraRouter router)

        {

            if (router == null)

                return;



            router.SetTravelerCameraTarget(transform);

            router.BindLocalTraveler(touchInput);

        }

    }

}


