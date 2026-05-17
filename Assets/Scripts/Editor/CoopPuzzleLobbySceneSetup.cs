using System;
using CoopPuzzle.Core.Bootstrap;
using CoopPuzzle.Lobby;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoopPuzzle.EditorTools
{
    public static class CoopPuzzleLobbySceneSetup
    {
        [MenuItem("Tools/CoopPuzzle/Setup/Setup Lobby Scene (One Click)")]
        public static void SetupLobbyScene()
        {
            try
            {
                var scene = SceneManager.GetActiveScene();
                if (!scene.isLoaded)
                    throw new InvalidOperationException("Aktif sahne yüklü değil.");

                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("CoopPuzzle Lobby Scene Setup");
                int undoGroup = Undo.GetCurrentGroup();

                // Create/Find core objects.
                var ugs = FindOrCreateInScene<UgsInitializer>("_UgsInitializer");
                var networkManager = FindOrCreateInScene<NetworkManager>("_NetworkManager");
                var transport = networkManager.GetComponent<UnityTransport>();
                if (transport == null)
                    transport = Undo.AddComponent<UnityTransport>(networkManager.gameObject);

                {
                    var nmSo = new SerializedObject(networkManager);
                    var configProp = nmSo.FindProperty("NetworkConfig");
                    var transportProp = configProp.FindPropertyRelative("NetworkTransport");
                    transportProp.objectReferenceValue = transport;
                    nmSo.ApplyModifiedPropertiesWithoutUndo();
                }

                var bootstrap = networkManager.GetComponent<NetworkBootstrap>();
                if (bootstrap == null)
                    bootstrap = Undo.AddComponent<NetworkBootstrap>(networkManager.gameObject);

                var coordinator = FindOrCreateInScene<LobbyCoordinator>("_LobbyCoordinator");

                // Wire LobbyCoordinator deps.
                {
                    var so = new SerializedObject(coordinator);
                    so.FindProperty("ugsInitializer").objectReferenceValue = ugs;
                    so.FindProperty("networkBootstrap").objectReferenceValue = bootstrap;

                    // Optional status text: try to find a TMP text named "Status" or "Durum".
                    var status = FindStatusTextCandidate();
                    so.FindProperty("statusText").objectReferenceValue = status;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // Wire NetworkBootstrap deps.
                {
                    var so = new SerializedObject(bootstrap);
                    so.FindProperty("networkManager").objectReferenceValue = networkManager;
                    so.FindProperty("unityTransport").objectReferenceValue = transport;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // Wire GameLobbyController -> LobbyCoordinator (keep existing UI scene).
                var gameLobbyController = UnityEngine.Object.FindAnyObjectByType<GameLobbyController>();
                if (gameLobbyController != null)
                {
                    var so = new SerializedObject(gameLobbyController);
                    so.FindProperty("lobbyCoordinator").objectReferenceValue = coordinator;
                    so.FindProperty("statusText").objectReferenceValue = FindStatusTextCandidate();
                    so.FindProperty("joinRoomCodeInput").objectReferenceValue =
                        FindInputFieldCandidate("OdaKodu", "LobbyCode", "JoinCode");
                    so.FindProperty("joinPlayerNameInput").objectReferenceValue =
                        FindInputFieldCandidate("Isim", "PlayerName", "OyuncuAdi");
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // Mark scene dirty so user can save.
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undoGroup);

                EditorUtility.DisplayDialog("CoopPuzzle", "Lobby sahnesi kuruldu.\nSahneyi kaydetmeyi unutma.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("CoopPuzzle - Hata", ex.Message, "OK");
            }
        }

        private static T FindOrCreateInScene<T>(string name) where T : Component
        {
            var existing = UnityEngine.Object.FindAnyObjectByType<T>();
            if (existing != null)
                return existing;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            return Undo.AddComponent<T>(go);
        }

        private static TMP_InputField FindInputFieldCandidate(params string[] names)
        {
            foreach (var input in UnityEngine.Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include))
            {
                if (input == null) continue;
                var n = (input.gameObject.name ?? string.Empty).Trim();
                foreach (var candidate in names)
                {
                    if (string.Equals(n, candidate, StringComparison.OrdinalIgnoreCase))
                        return input;
                }
            }

            return null;
        }

        private static TextMeshProUGUI FindStatusTextCandidate()
        {
            // Try common names first (fast path).
            foreach (var t in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
            {
                if (t == null) continue;
                var n = (t.gameObject.name ?? string.Empty).Trim();
                if (string.Equals(n, "Status", StringComparison.OrdinalIgnoreCase)) return t;
                if (string.Equals(n, "Durum", StringComparison.OrdinalIgnoreCase)) return t;
                if (string.Equals(n, "StatusText", StringComparison.OrdinalIgnoreCase)) return t;
                if (string.Equals(n, "DurumText", StringComparison.OrdinalIgnoreCase)) return t;
            }

            return null;
        }
    }
}

