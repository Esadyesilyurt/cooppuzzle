using System.IO;
using CoopPuzzle.Questions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoopPuzzle.EditorTools
{
    /// <summary>
    /// UI yerleşimine dokunmaz. Varsayılan olarak SampleScene'e sadece non-UI root ekler (mevcut objelere dokunmaz).
    /// </summary>
    public static class CoopPuzzlePhase2OneClickSetup
    {
        private const string TargetScenePath = "Assets/Scenes/SampleScene.unity";
        private const string QuestionsRootName = "_CoopPuzzle_Questions";

        [MenuItem("Tools/CoopPuzzle/Setup/Phase 2 Setup (One Click)")]
        public static void SetupPhase2()
        {
            if (CoopPuzzleEditorPlayModeGuard.BlockIfPlaying("Phase 2 Setup"))
                return;

            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/ScriptableObjects/Questions");
            EnsureFolder("Assets/Prefabs");

            CreateOrLoadTargetScene();
            EnsureQuestionSystemInActiveScene();

            var db = EnsureSampleQuestionDatabase();
            WireQuestionManager(db);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "CoopPuzzle",
                "Phase 2 kurulum tamam.\n\n" +
                "- SampleScene güncellendi (veya hedef sahne oluşturuldu)\n" +
                "- Örnek QuestionDatabase + örnek sorular oluşturuldu (yoksa)\n" +
                "- QuestionManager + kapı slotları eklendi\n\n" +
                "Sahneyi ve assetleri kaydetmeyi unutma.",
                "OK");
        }

        private static void CreateOrLoadTargetScene()
        {
            if (File.Exists(TargetScenePath))
            {
                EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, TargetScenePath);
            EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        }

        private static void EnsureQuestionSystemInActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            var existingRoot = GameObject.Find(QuestionsRootName);
            if (existingRoot != null)
                return;

            var root = new GameObject(QuestionsRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Questions Root");

            var manager = Undo.AddComponent<QuestionManager>(root);

            // Örnek kapı slotları: gerçek labirentte bu child'ları prefab kapına taşıyabilirsin.
            for (int i = 0; i < 4; i++)
            {
                var doorGo = new GameObject($"DoorSlot_{i + 1}");
                Undo.RegisterCreatedObjectUndo(doorGo, $"Create {doorGo.name}");
                doorGo.transform.SetParent(root.transform, false);
                Undo.AddComponent<DoorQuestionSlot>(doorGo);
            }

            // Manager'da kapı listesi inspector'dan da doldurulabilir; burada güvenli bağlama yapalım.
            var so = new SerializedObject(manager);
            var doorsProp = so.FindProperty("doorSlots");
            doorsProp.ClearArray();

            var slots = root.GetComponentsInChildren<DoorQuestionSlot>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                doorsProp.InsertArrayElementAtIndex(i);
                doorsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static QuestionDatabase EnsureSampleQuestionDatabase()
        {
            const string path = "Assets/ScriptableObjects/Questions/QuestionDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<QuestionDatabase>(path);
            if (db != null)
                return db;

            db = ScriptableObject.CreateInstance<QuestionDatabase>();
            AssetDatabase.CreateAsset(db, path);

            // Örnek birkaç soru asset'i
            AddSampleQuestion(path.Replace("QuestionDatabase.asset", "Sample_01.asset"),
                "Türkiye'nin başkenti neresidir?", new[] { "İstanbul", "Ankara", "İzmir", "Bursa" }, 1);
            AddSampleQuestion(path.Replace("QuestionDatabase.asset", "Sample_02.asset"),
                "Hangi gezegen Güneş'e en yakındır?", new[] { "Venüs", "Merkür", "Mars", "Jüpiter" }, 1);
            AddSampleQuestion(path.Replace("QuestionDatabase.asset", "Sample_03.asset"),
                "2 + 2 kaçtır?", new[] { "3", "4", "5", "22" }, 1);

            // DB listesine ScriptableObject referansları bağlamak: reflection ile SerializedObject
            var dbSo = new SerializedObject(db);
            var list = dbSo.FindProperty("questions");
            list.ClearArray();

            var guids = AssetDatabase.FindAssets("t:QuestionData", new[] { "Assets/ScriptableObjects/Questions" });
            int idx = 0;
            foreach (var guid in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var q = AssetDatabase.LoadAssetAtPath<QuestionData>(p);
                if (q == null) continue;

                list.InsertArrayElementAtIndex(idx);
                list.GetArrayElementAtIndex(idx).objectReferenceValue = q;
                idx++;
            }

            dbSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(db);
            return db;
        }

        private static void AddSampleQuestion(string assetPath, string text, string[] options, int correctIndex)
        {
            if (File.Exists(assetPath))
                return;

            EnsureFolder(Path.GetDirectoryName(assetPath)!.Replace("\\", "/"));
            var q = ScriptableObject.CreateInstance<QuestionData>();

            var so = new SerializedObject(q);
            so.FindProperty("questionText").stringValue = text;

            var opts = so.FindProperty("options");
            opts.ClearArray();
            for (int i = 0; i < options.Length; i++)
            {
                opts.InsertArrayElementAtIndex(i);
                opts.GetArrayElementAtIndex(i).stringValue = options[i];
            }

            so.FindProperty("correctOptionIndex").intValue = correctIndex;
            so.FindProperty("category").stringValue = "Sample";
            so.FindProperty("difficulty").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(q, assetPath);
        }

        private static void WireQuestionManager(QuestionDatabase db)
        {
            var root = GameObject.Find(QuestionsRootName);
            if (root == null) return;

            var manager = root.GetComponent<QuestionManager>();
            if (manager == null) return;

            var so = new SerializedObject(manager);
            so.FindProperty("database").objectReferenceValue = db;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string unityPath)
        {
            if (AssetDatabase.IsValidFolder(unityPath))
                return;

            var parent = Path.GetDirectoryName(unityPath)?.Replace('\\', '/');
            var name = Path.GetFileName(unityPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                return;

            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
