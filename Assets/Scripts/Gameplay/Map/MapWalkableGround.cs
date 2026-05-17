using UnityEngine;

namespace CoopPuzzle.Gameplay.Map
{
    /// <summary>
    /// Harita altındaki yürünebilir zemin slab'ı. NavMesh + raycast için kullanılır.
    /// </summary>
    public sealed class MapWalkableGround : MonoBehaviour
    {
        [SerializeField] private float paddingXZ = 2f;
        [SerializeField] private float thickness = 0.25f;
        [SerializeField] private float belowMapOffset = 0.05f;

        public float PaddingXZ => paddingXZ;
        public float Thickness => thickness;
        public float BelowMapOffset => belowMapOffset;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.35f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }
#endif
    }
}
