using CoopPuzzle.Gameplay.Player;
using CoopPuzzle.Gameplay.Sage;
using CoopPuzzle.Gameplay.UI;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Core
{
    /// <summary>
    /// Yerel oyuncu rolüne göre kamera ve UI (Gezgin / Bilge ayrı oyuncular — lobby'den atanır).
    /// </summary>
    public sealed class GameplayCameraRouter : MonoBehaviour
    {
        [SerializeField] private GameplaySessionConfig session;
        [SerializeField] private UnityEngine.Camera travelerCamera;
        [SerializeField] private UnityEngine.Camera sageCamera;
        [SerializeField] private TravelerTouchInput travelerInput;
        [SerializeField] private GameplayQuestionUI travelerQuestionUI;
        [SerializeField] private SageDocumentUI sageDocumentUI;
        [SerializeField] private SageDocumentFlowController sageDocumentFlow;

        private GameplayRole _appliedRole = (GameplayRole)(-1);

        private void Awake()
        {
            if (session == null)
                session = GameplaySessionConfig.Instance;

            if (travelerQuestionUI == null)
                travelerQuestionUI = FindAnyObjectByType<GameplayQuestionUI>();

            if (sageDocumentUI == null)
                sageDocumentUI = FindAnyObjectByType<SageDocumentUI>();

            if (sageDocumentFlow == null)
                sageDocumentFlow = FindAnyObjectByType<SageDocumentFlowController>();
        }

        private void Start() => ApplyRole();

        public void ApplyRole()
        {
            if (session == null) return;

            _appliedRole = session.LocalRole;
            var isTraveler = _appliedRole == GameplayRole.Traveler;

            if (travelerCamera != null)
                travelerCamera.enabled = isTraveler;

            if (sageCamera != null)
                sageCamera.enabled = !isTraveler;

            if (travelerInput != null)
                travelerInput.enabled = isTraveler;

            SetCanvasEnabled(travelerQuestionUI, isTraveler);
            SetCanvasEnabled(sageDocumentUI, !isTraveler);

            if (isTraveler)
            {
                sageDocumentUI?.Hide();
            }
            else
            {
                travelerQuestionUI?.Hide();
                sageDocumentFlow?.RefreshActiveDocument();
            }
        }

        private static void SetCanvasEnabled(Component uiComponent, bool enabled)
        {
            if (uiComponent == null) return;
            var canvas = uiComponent.GetComponentInParent<Canvas>(true);
            if (canvas != null)
                canvas.enabled = enabled;
        }
    }
}
