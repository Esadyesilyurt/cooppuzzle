using CoopPuzzle.Gameplay.Core;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoopPuzzle.EditorTools
{
    public static class CoopPuzzlePhase5OneClickSetup
    {
        private const string LobbyScenePath = "Assets/Scenes/menü.unity";
        private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/CoopPuzzle/Setup/Phase 5 Setup (Lobby → Game)")]
        public static void SetupPhase5()
        {
            if (CoopPuzzleEditorPlayModeGuard.BlockIfPlaying("Phase 5 Setup"))
                return;

            EnsureScenesInBuildSettings();
            ConfigureNetworkManagerInLobbyScene();
            EnsureGameplayBootstrapInSampleScene();

            EditorUtility.DisplayDialog(
                "CoopPuzzle — Phase 5",
                "Lobby → Oyun sahnesi akışı hazır.\n\n" +
                "1) menü sahnesinde Host/Join\n" +
                "2) Host UI'da bir butona GameLobbyController.StartGame bağla\n" +
                "3) Tüm oyuncular SampleScene'e gider\n" +
                "4) Rol: slot 0,2 Gezgin | 1,3 Bilge (varsayılan)\n\n" +
                "ParrelSync: 4 editör ile tam test.",
                "OK");
        }

        private static void EnsureScenesInBuildSettings()
        {
            var scenes = new[]
            {
                LobbyScenePath,
                GameplayScenePath
            };

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var path in scenes)
            {
                if (!System.IO.File.Exists(path)) continue;
                list.Add(new EditorBuildSettingsScene(path, true));
            }

            if (list.Count > 0)
                EditorBuildSettings.scenes = list.ToArray();
        }

        private static void ConfigureNetworkManagerInLobbyScene()
        {
            if (!System.IO.File.Exists(LobbyScenePath)) return;

            var active = SceneManager.GetActiveScene();
            EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);

            var networkManager = Object.FindAnyObjectByType<NetworkManager>();
            if (networkManager != null)
            {
                var so = new SerializedObject(networkManager);
                var configProp = so.FindProperty("NetworkConfig");
                if (configProp != null)
                {
                    var enableScenes = configProp.FindPropertyRelative("EnableSceneManagement");
                    if (enableScenes != null)
                        enableScenes.boolValue = true;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(networkManager);
            }

            CoopPuzzleLobbySceneSetup.SetupLobbyScene();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            if (!string.IsNullOrEmpty(active.path) && active.path != LobbyScenePath)
                EditorSceneManager.OpenScene(active.path, OpenSceneMode.Single);
        }

        private static void EnsureGameplayBootstrapInSampleScene()
        {
            if (!System.IO.File.Exists(GameplayScenePath)) return;

            var active = SceneManager.GetActiveScene();
            EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

            var root = GameObject.Find("_CoopPuzzle_Gameplay");
            if (root != null && root.GetComponent<GameplaySessionBootstrap>() == null)
                Undo.AddComponent<GameplaySessionBootstrap>(root);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            if (!string.IsNullOrEmpty(active.path) && active.path != GameplayScenePath)
                EditorSceneManager.OpenScene(active.path, OpenSceneMode.Single);
        }
    }
}
