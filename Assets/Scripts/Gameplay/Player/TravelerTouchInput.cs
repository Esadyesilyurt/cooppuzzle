using CoopPuzzle.Gameplay.Doors;
using CoopPuzzle.Gameplay.Questions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CoopPuzzle.Gameplay.Player
{
    /// <summary>
    /// Mobil dokunma + Editor mouse ile point & click. Önce kapı, sonra zemin.
    /// </summary>
    public sealed class TravelerTouchInput : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera inputCamera;
        [SerializeField] private TravelerMovementController movement;
        [SerializeField] private float maxRayDistance = 200f;
        [SerializeField] private LayerMask rayMask = ~0;

        [Header("Soru açıkken hareket")]
        [SerializeField] private bool blockMovementWhileQuestionOpen = true;

        private QuestionFlowController _questionFlow;

        public void SetInputCamera(UnityEngine.Camera camera)
        {
            if (camera != null)
                inputCamera = camera;
        }

        private void Awake()
        {
            if (inputCamera == null)
                inputCamera = UnityEngine.Camera.main;

            if (movement == null)
                movement = GetComponent<TravelerMovementController>();

            _questionFlow = FindAnyObjectByType<QuestionFlowController>();
        }

        private void Update()
        {
            if (blockMovementWhileQuestionOpen && _questionFlow != null && _questionFlow.BlocksWorldInput)
                return;

            if (!TryGetTapPosition(out var screenPos))
                return;

            if (IsPointerOverUi())
                return;

            if (inputCamera == null || movement == null)
                return;

            var ray = inputCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out var hit, maxRayDistance, rayMask, QueryTriggerInteraction.Collide))
                return;

            var door = hit.collider.GetComponentInParent<DoorInteractable>();
            if (door != null)
            {
                door.TryInteractFromTraveler(transform);
                return;
            }

            movement.TryMoveTo(hit.point);
        }

        private static bool TryGetTapPosition(out Vector2 screenPos)
        {
            screenPos = default;

            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                if (touch.press.wasReleasedThisFrame)
                {
                    screenPos = touch.position.ReadValue();
                    return true;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasReleasedThisFrame)
            {
                screenPos = mouse.position.ReadValue();
                return true;
            }

            return false;
        }

        private static bool IsPointerOverUi()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasReleasedThisFrame)
            {
                var touchId = touchscreen.primaryTouch.touchId.ReadValue();
                return eventSystem.IsPointerOverGameObject(touchId);
            }

            return eventSystem.IsPointerOverGameObject();
        }
    }
}
