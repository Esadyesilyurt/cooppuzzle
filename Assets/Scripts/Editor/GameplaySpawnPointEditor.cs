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

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Bake Spawn Position (NavMesh)"))
                sp.BakeSpawnPosition();

            var spawnPos = sp.GetSpawnPosition();
            var drift = Vector3.Distance(
                new Vector3(sp.transform.position.x, 0f, sp.transform.position.z),
                new Vector3(spawnPos.x, 0f, spawnPos.z));

            EditorGUILayout.HelpBox(
                $"Gezgin spawn — {sp.Team}\n" +
                $"Bake konum: {spawnPos}\n" +
                $"Marker kayması (yatay): {drift:F2} m\n\n" +
                "Sarı çizgi = marker ile bake farkı.\n" +
                "Spawn kapıya kayıyorsa: marker'ı yeşil zemine taşı → Bake.",
                drift > 0.75f ? MessageType.Warning : MessageType.Info);
        }

        private void OnSceneGUI()
        {
            var sp = (GameplaySpawnPoint)target;
            var spawnPos = sp.GetSpawnPosition();
            var color = sp.Team == SpawnTeam.Team1 ? Color.red : Color.cyan;

            Handles.color = color;
            Handles.ArrowHandleCap(0, spawnPos, sp.transform.rotation, 1.5f, EventType.Repaint);
            Handles.Label(spawnPos + Vector3.up * 1.4f, $"{sp.Team} · Gezgin (spawn)");
        }
    }
}
