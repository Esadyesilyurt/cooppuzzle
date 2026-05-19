using CoopPuzzle.Gameplay.Core;
using CoopPuzzle.Gameplay.Doors;
using CoopPuzzle.Gameplay.Map;
using UnityEditor;
using UnityEngine;

namespace CoopPuzzle.EditorTools
{
    [CustomEditor(typeof(GameplaySessionConfig))]
    public sealed class GameplaySessionConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "Online oyunda rol lobby'den gelir (Tab ile değiştirme YOK).\n\n" +
                "Yerel test:\n" +
                "• Traveler = soru paneli + harita kontrolü\n" +
                "• Sage = belge + Gezgin kamerası\n\n" +
                "İki build ile test: biri Traveler, biri Sage.",
                MessageType.Info);

            if (!Application.isPlaying)
                return;

            if (GUILayout.Button("Rol UI / Kamerayı Yenile"))
            {
                var router = FindAnyObjectByType<GameplayCameraRouter>();
                router?.ApplyRole();
            }
        }

        [MenuItem("Tools/CoopPuzzle/Test/Play As Traveler (set before Play)")]
        private static void SetTravelerRole() => SetRole(GameplayRole.Traveler);

        [MenuItem("Tools/CoopPuzzle/Test/Play As Sage (set before Play)")]
        private static void SetSageRole() => SetRole(GameplayRole.Sage);

        [MenuItem("Tools/CoopPuzzle/Test/Simulate Traveler Door (Sage belge önizleme)")]
        private static void SimulateDoorForSagePreview()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("CoopPuzzle", "Bu test yalnızca Play modunda çalışır.", "OK");
                return;
            }

            var door = Object.FindAnyObjectByType<DoorInteractable>();
            if (door == null)
            {
                EditorUtility.DisplayDialog("CoopPuzzle", "Sahnede kapı yok.", "OK");
                return;
            }

            var data = door.GetQuestion();
            if (data == null)
            {
                EditorUtility.DisplayDialog("CoopPuzzle", "Kapıya soru atanmamış.", "OK");
                return;
            }

            var config = FindSessionConfig();
            var team = config != null ? config.LocalTeam : CoopPuzzle.Gameplay.Map.SpawnTeam.Team1;
            CoopPuzzle.Gameplay.Core.DoorGameplayEvents.RaiseQuestionStarted(door, data, team);
            var router = Object.FindAnyObjectByType<GameplayCameraRouter>();
            router?.ApplyRole();
        }

        private static void SetRole(GameplayRole role)
        {
            var config = FindSessionConfig();
            if (config == null)
            {
                EditorUtility.DisplayDialog(
                    "CoopPuzzle",
                    "GameplaySessionConfig bulunamadı.\nPhase 4 Setup çalıştır.",
                    "OK");
                return;
            }

            Undo.RecordObject(config, "Set local role");
            config.SetLocalRole(role);
            EditorUtility.SetDirty(config);

            if (Application.isPlaying)
            {
                var router = FindAnyObjectByType<GameplayCameraRouter>();
                router?.ApplyRole();
            }

            Debug.Log($"[CoopPuzzle] Yerel rol ayarlandı: {role} (Play'e basmadan önce kaydet)");
        }

        private static GameplaySessionConfig FindSessionConfig()
        {
            if (Application.isPlaying && GameplaySessionConfig.Instance != null)
                return GameplaySessionConfig.Instance;

            return Object.FindAnyObjectByType<GameplaySessionConfig>(FindObjectsInactive.Include);
        }
    }
}
