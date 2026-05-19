using CoopPuzzle.Gameplay.Camera;

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



            ResolveCamerasByName();



            if (travelerQuestionUI == null)

                travelerQuestionUI = FindAnyObjectByType<GameplayQuestionUI>();



            if (sageDocumentUI == null)

                sageDocumentUI = FindAnyObjectByType<SageDocumentUI>();



            if (sageDocumentFlow == null)

                sageDocumentFlow = FindAnyObjectByType<SageDocumentFlowController>();

        }



        private void Start()

        {

            ApplyRole();

            TryBindLocalNetworkTraveler();

        }



        /// <summary>Inspector'da ters bağlanmış kameraları isimle düzeltir.</summary>

        private void ResolveCamerasByName()

        {

            UnityEngine.Camera gameplayCam = null;

            UnityEngine.Camera sageCam = null;



            foreach (var cam in FindObjectsByType<UnityEngine.Camera>(FindObjectsInactive.Include))

            {

                if (cam == null)

                    continue;



                if (cam.gameObject.name == "GameplayCamera")

                    gameplayCam = cam;

                else if (cam.gameObject.name == "SageCamera")

                    sageCam = cam;

            }



            if (gameplayCam != null)

                travelerCamera = gameplayCam;



            if (sageCam != null)

                sageCamera = sageCam;

        }



        /// <summary>Spawn sonrası yerel Gezgin kamerasını PlayerObject'e bağlar.</summary>

        public void TryBindLocalNetworkTraveler()

        {

            if (session == null || session.LocalRole != GameplayRole.Traveler)

                return;



            var localTraveler = LocalPlayerLookup.GetLocalTraveler();

            if (localTraveler == null)

                return;



            SetTravelerCameraTarget(localTraveler.transform);

            BindLocalTraveler(localTraveler.GetComponent<TravelerTouchInput>());

        }



        public UnityEngine.Camera TravelerCamera => travelerCamera;



        public UnityEngine.Camera SageCamera => sageCamera;



        public void BindLocalTraveler(TravelerTouchInput input)

        {

            if (input != null)

            {

                travelerInput = input;

                if (travelerCamera != null)

                    input.SetInputCamera(travelerCamera);

            }



            ApplyRole();

        }



        public void SetTravelerCameraTarget(Transform target)

        {

            if (travelerCamera == null)

                return;



            var follow = travelerCamera.GetComponent<TopDownCameraFollow>();

            if (follow != null)

                follow.SetTarget(target);

        }



        public void ClearTravelerCameraTarget() => SetTravelerCameraTarget(null);



        public void SetSageCameraTarget(Transform target)

        {

            if (target == null || sageCamera == null)

                return;



            var follow = sageCamera.GetComponent<TopDownCameraFollow>();

            follow?.SetTarget(target);

        }



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

            {

                var netTraveler = travelerInput.GetComponent<NetworkTravelerController>();

                travelerInput.enabled = isTraveler && (netTraveler == null || netTraveler.IsLocallyControlled);

            }



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


