using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoopPuzzle.EditorTools
{
    public static class CoopPuzzleEditorMissingScriptCleanup
    {
        [MenuItem("Tools/CoopPuzzle/Setup/Remove Missing Scripts (Gameplay Root)")]
        public static void RemoveMissingOnGameplayRoot()
        {
            if (CoopPuzzleEditorPlayModeGuard.BlockIfPlaying("Missing script temizliği"))
                return;

            var root = GameObject.Find("_CoopPuzzle_Gameplay");
            if (root == null)
            {
                EditorUtility.DisplayDialog("CoopPuzzle", "_CoopPuzzle_Gameplay bulunamadı.", "OK");
                return;
            }

            var removed = RemoveMissingRecursive(root);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            EditorUtility.DisplayDialog(
                "CoopPuzzle",
                removed > 0
                    ? $"Kaldırılan eksik script bileşeni: {removed}\n\n(Sahneyi kaydet.)"
                    : "Eksik script bulunamadı.",
                "OK");
        }

        [MenuItem("Tools/CoopPuzzle/Setup/Remove Missing Scripts (All Loaded Scenes)")]
        public static void RemoveMissingInAllLoadedScenes()
        {
            if (CoopPuzzleEditorPlayModeGuard.BlockIfPlaying("Missing script temizliği"))
                return;

            var total = 0;
            for (int i = 0; i < SceneManager.loadedSceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                foreach (var rootGo in scene.GetRootGameObjects())
                    total += RemoveMissingRecursive(rootGo);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("CoopPuzzle", $"Toplam kaldırılan eksik script: {total}", "OK");
        }

        private static int RemoveMissingRecursive(GameObject go)
        {
            var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (count > 0)
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

            foreach (Transform child in go.transform)
                count += RemoveMissingRecursive(child.gameObject);

            return count;
        }
    }
}
