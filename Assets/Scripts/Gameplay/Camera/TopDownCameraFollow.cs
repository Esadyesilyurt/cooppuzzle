using UnityEngine;

namespace CoopPuzzle.Gameplay.Camera
{
    public sealed class TopDownCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private UnityEngine.Camera viewCamera;
        [SerializeField] private Vector3 offset = new(0f, 10f, -5.5f);
        [SerializeField] private float orthographicSize = 7.5f;
        [SerializeField] private float followSmooth = 10f;
        [SerializeField] private bool lookAtTarget = true;

        private void Awake()
        {
            if (viewCamera == null)
                viewCamera = GetComponent<UnityEngine.Camera>();

            ApplyOrthographicSize();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            var desired = target.position + offset;
            transform.position = Vector3.Lerp(
                transform.position,
                desired,
                1f - Mathf.Exp(-followSmooth * Time.deltaTime));

            if (lookAtTarget)
                transform.LookAt(target.position + Vector3.up * 0.5f);
        }

        public void SetTarget(Transform newTarget) => target = newTarget;

        public void ApplyOrthographicSize()
        {
            if (viewCamera != null && viewCamera.orthographic)
                viewCamera.orthographicSize = orthographicSize;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (viewCamera == null)
                viewCamera = GetComponent<UnityEngine.Camera>();

            ApplyOrthographicSize();
        }
#endif
    }
}
