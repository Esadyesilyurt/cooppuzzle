using CoopPuzzle.Gameplay.Map;
using UnityEditor;
using UnityEngine;

namespace CoopPuzzle.EditorTools
{
    [CustomEditor(typeof(GameplaySpawnPoint))]
    public sealed class GameplaySpawnPointEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var sp = (GameplaySpawnPoint)target;
            EditorGUILayout.HelpBox(
                $"Gezgin spawn — {sp.Team}\n\n" +
                "Bilge için spawn yok. Bilge, takımındaki Gezgin'i izler ve " +
                "tek ana belgede arama yapar (Phase 4).",
                MessageType.Info);
        }

        private void OnSceneGUI()
        {
            var sp = (GameplaySpawnPoint)target;
            Handles.color = Color.cyan;
            Handles.ArrowHandleCap(
                0,
                sp.transform.position,
                sp.transform.rotation,
                1.5f,
                EventType.Repaint);
            Handles.Label(sp.transform.position + Vector3.up * 1.4f, $"{sp.Team} · Gezgin");
        }
    }
}
