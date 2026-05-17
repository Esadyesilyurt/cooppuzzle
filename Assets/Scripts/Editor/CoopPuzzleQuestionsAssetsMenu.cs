using System.IO;
using CoopPuzzle.Questions;
using UnityEditor;
using UnityEngine;

namespace CoopPuzzle.EditorTools
{
    public static class CoopPuzzleQuestionsAssetsMenu
    {
        [MenuItem("Tools/CoopPuzzle/Sage/Create Master Document Asset")]
        public static void CreateMasterDocument()
        {
            const string folder = "Assets/ScriptableObjects/Sage";
            EnsureFolder(folder);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/SageMasterDocument.asset");
            var doc = ScriptableObject.CreateInstance<SageMasterDocument>();
            AssetDatabase.CreateAsset(doc, path);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("CoopPuzzle", $"Ana Bilge belgesi:\n{path}", "OK");
            Selection.activeObject = doc;
        }

        [MenuItem("Tools/CoopPuzzle/Questions/Create Sample Question Database")]
        public static void CreateSampleDatabase()
        {
            const string folder = "Assets/ScriptableObjects/Questions";
            EnsureFolder(folder);

            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/QuestionDatabase.asset");
            var db = ScriptableObject.CreateInstance<QuestionDatabase>();
            AssetDatabase.CreateAsset(db, assetPath);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("CoopPuzzle", $"QuestionDatabase oluşturuldu:\n{assetPath}", "OK");
            Selection.activeObject = db;
        }

        private static void EnsureFolder(string unityPath)
        {
            if (AssetDatabase.IsValidFolder(unityPath)) return;

            var parent = Path.GetDirectoryName(unityPath)?.Replace('\\', '/');
            var name = Path.GetFileName(unityPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;

            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}

