using CoopPuzzle.Gameplay.Doors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace CoopPuzzle.EditorTools
{
    public static class CoopPuzzleDoorBlockingFix
    {
        [MenuItem("Tools/CoopPuzzle/Map/Fix Door Blocking (NavMesh + Collider)")]
        public static void FixAllDoorsInScene()
        {
            if (CoopPuzzleEditorPlayModeGuard.BlockIfPlaying("Fix door blocking"))
                return;

            var fixedCount = 0;
            foreach (var door in Object.FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include))
            {
                if (door == null)
                    continue;

                EnsureDoorBlocker(door.gameObject);
                fixedCount++;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog(
                "CoopPuzzle",
                $"Kapı engelleri güncellendi: {fixedCount}\n\n" +
                "• DoorBlocker collider (solid)\n" +
                "• NavMeshObstacle carving\n\n" +
                "NavMesh'i yeniden bake etmek gerekebilir:\n" +
                "Tools → CoopPuzzle → Map → Bake NavMesh",
                "OK");
        }

        public static void EnsureDoorBlocker(GameObject doorRoot)
        {
            var blocker = doorRoot.transform.Find("DoorBlocker");
            if (blocker == null)
            {
                var go = new GameObject("DoorBlocker");
                Undo.RegisterCreatedObjectUndo(go, "Door blocker");
                go.transform.SetParent(doorRoot.transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                blocker = go.transform;
            }

            var box = blocker.GetComponent<BoxCollider>();
            if (box == null)
                box = Undo.AddComponent<BoxCollider>(blocker.gameObject);

            box.size = new Vector3(0.4f, 2.2f, 1.7f);
            box.isTrigger = false;
            box.enabled = true;

            var obstacle = doorRoot.GetComponent<NavMeshObstacle>();
            if (obstacle == null)
                obstacle = Undo.AddComponent<NavMeshObstacle>(doorRoot);

            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = Vector3.zero;
            obstacle.size = box.size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
            obstacle.enabled = true;

            var door = doorRoot.GetComponent<DoorInteractable>();
            if (door != null)
            {
                var dso = new SerializedObject(door);
                dso.FindProperty("blockingCollider").objectReferenceValue = box;
                dso.FindProperty("navMeshObstacle").objectReferenceValue = obstacle;
                dso.ApplyModifiedPropertiesWithoutUndo();
            }

            var leaf = doorRoot.transform.Find("Leaf");
            if (leaf != null)
            {
                var leafCol = leaf.GetComponent<Collider>();
                if (leafCol != null)
                    leafCol.isTrigger = false;
            }
        }
    }
}
