using CoopPuzzle.Gameplay.Player;
using CoopPuzzle.Gameplay.Questions;
using CoopPuzzle.Questions;
using UnityEngine;
using UnityEngine.AI;

namespace CoopPuzzle.Gameplay.Doors
{
    [RequireComponent(typeof(DoorQuestionSlot))]
    public sealed class DoorInteractable : MonoBehaviour
    {
        [SerializeField] private DoorQuestionSlot questionSlot;
        [SerializeField] private Collider blockingCollider;
        [SerializeField] private NavMeshObstacle navMeshObstacle;
        [SerializeField] private float interactDistance = 2.5f;
        [SerializeField] private Vector3 blockerSize = new(0.4f, 2.2f, 1.7f);

        [Header("Görsel (opsiyonel)")]
        [SerializeField] private Transform doorLeaf;

        private bool _isOpen;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            DoorRegistry.Register(this);

            if (questionSlot == null)
                questionSlot = GetComponent<DoorQuestionSlot>();

            EnsureBlockingVolume();
            ApplyClosedPhysics();
        }

        private void OnDestroy() => DoorRegistry.Unregister(this);

        public void TryInteractFromTraveler(Transform travelerTransform)
        {
            if (_isOpen || travelerTransform == null)
                return;

            var dist = Vector3.Distance(travelerTransform.position, transform.position);
            if (dist > interactDistance)
            {
                Debug.Log("Kapıya daha yaklaş.");
                return;
            }

            var movement = travelerTransform.GetComponent<TravelerMovementController>();
            if (movement == null)
            {
                Debug.LogWarning("Kapı etkileşimi: TravelerMovementController yok.");
                return;
            }

            var flow = FindAnyObjectByType<QuestionFlowController>();
            if (flow == null)
            {
                Debug.LogError("QuestionFlowController sahnede yok.");
                return;
            }

            flow.RequestQuestion(this, movement);
        }

        public QuestionData GetQuestion() => questionSlot != null ? questionSlot.AssignedQuestion : null;

        public void Open()
        {
            if (_isOpen)
                return;

            _isOpen = true;
            ApplyOpenVisuals();
        }

        private void ApplyOpenVisuals()
        {
            if (navMeshObstacle != null)
                navMeshObstacle.enabled = false;

            gameObject.SetActive(false);
        }

        private void ApplyClosedPhysics()
        {
            if (_isOpen)
                return;

            if (blockingCollider != null)
            {
                blockingCollider.enabled = true;
                blockingCollider.isTrigger = false;
            }

            if (navMeshObstacle != null)
            {
                navMeshObstacle.enabled = true;
                navMeshObstacle.carving = true;
                navMeshObstacle.carveOnlyStationary = true;
            }
        }

        private void EnsureBlockingVolume()
        {
            var blocker = transform.Find("DoorBlocker");
            if (blocker == null)
            {
                var go = new GameObject("DoorBlocker");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;

                var box = go.AddComponent<BoxCollider>();
                box.size = blockerSize;
                box.isTrigger = false;
                blockingCollider = box;
            }
            else if (blockingCollider == null)
            {
                blockingCollider = blocker.GetComponent<Collider>();
            }

            if (blockingCollider != null)
                blockingCollider.isTrigger = false;

            if (navMeshObstacle == null)
                navMeshObstacle = GetComponent<NavMeshObstacle>();

            if (navMeshObstacle == null)
                navMeshObstacle = gameObject.AddComponent<NavMeshObstacle>();

            navMeshObstacle.shape = NavMeshObstacleShape.Box;
            navMeshObstacle.center = Vector3.zero;
            navMeshObstacle.size = blockerSize;
            navMeshObstacle.carving = true;
            navMeshObstacle.carveOnlyStationary = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (blockerSize.x < 0.2f)
                blockerSize = new Vector3(0.4f, 2.2f, 1.7f);
        }
#endif
    }
}
