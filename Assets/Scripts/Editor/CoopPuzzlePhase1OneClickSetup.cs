using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CoopPuzzle.EditorTools
{
    public static class CoopPuzzlePhase1OneClickSetup
    {
        [MenuItem("Tools/CoopPuzzle/Setup/Phase 1 Setup (One Click)")]
        public static void SetupPhase1()
        {
            try
            {
                EnsureFolder("Assets/Prefabs");
                EnsureFolder("Assets/Scenes");
                EnsureFolder("Assets/ScriptableObjects");
                EnsureFolder("Assets/Scripts");
                EnsureFolder("Assets/Scripts/Core");
                EnsureFolder("Assets/Scripts/Core/Bootstrap");
                EnsureFolder("Assets/Scripts/Lobby");
                EnsureFolder("Assets/Scripts/UI");
                EnsureFolder("Assets/Scripts/UI/Lobby");
                EnsureFolder("Assets/Scripts/Editor");

                ConfigureLandscapeMobile();

                // This does NOT touch UI layout. It only creates/links non-UI bootstrap objects,
                // and wires existing GameLobbyController references if present.
                CoopPuzzleLobbySceneSetup.SetupLobbyScene();

                EditorUtility.DisplayDialog(
                    "CoopPuzzle",
                    "Phase 1 kurulum tamam.\n\n- Klasör yapısı hazır\n- Mobil yatay (landscape) ayarlandı\n- Lobby sahnesi bootstrap objeleri kuruldu\n\nSahneyi kaydetmeyi unutma.",
                    "OK"
                );
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("CoopPuzzle - Hata", ex.Message, "OK");
            }
        }

        [MenuItem("Tools/CoopPuzzle/Setup/Configure Mobile (Landscape)")]
        public static void ConfigureLandscapeMobile()
        {
            // Mobile + landscape-only defaults (does not change UI hierarchy/layout).
#if UNITY_2021_3_OR_NEWER
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
#endif

            // Optional: keep safe area handling to UI layer later; no forced changes here.
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolder(string unityPath)
        {
            if (AssetDatabase.IsValidFolder(unityPath))
                return;

            var parent = Path.GetDirectoryName(unityPath)?.Replace('\\', '/');
            var name = Path.GetFileName(unityPath);

            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                throw new InvalidOperationException($"Geçersiz klasör yolu: {unityPath}");

            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}

