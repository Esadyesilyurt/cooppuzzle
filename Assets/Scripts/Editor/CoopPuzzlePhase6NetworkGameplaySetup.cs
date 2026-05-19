using CoopPuzzle.Gameplay.Core;
using CoopPuzzle.Gameplay.Map;
using CoopPuzzle.Gameplay.Player;
using CoopPuzzle.Gameplay.UI;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace CoopPuzzle.EditorTools
{
    public static class CoopPuzzlePhase6NetworkGameplaySetup
    {
        private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";
        private const string LobbyScenePath = "Assets/Scenes/menü.unity";
        private const string PrefabPath = "Assets/Prefabs/NetworkTraveler.prefab";
        private const string PrefabListPath = "Assets/Prefabs/CoopPuzzleNetworkPrefabs.asset";

        [MenuItem("Tools/CoopPuzzle/Setup/Phase 6 Setup (Networked Players)")]
        public static void SetupPhase6()
        {
            if (CoopPuzzleEditorPlayModeGuard.BlockIfPlaying("Phase 6 Setup"))
                return;

            EnsureFolder("Assets/Prefabs");

            var prefab = CreateOrUpdateNetworkTravelerPrefab();
            var prefabList = CreateOrUpdatePrefabList(prefab);
            ConfigureGameplayScene(prefab);
            ConfigureLobbyNetworkManager(prefabList);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "CoopPuzzle — Phase 6",
                "Ağ üzerinden oyuncu spawn hazır.\n\n" +
                "• NetworkTraveler prefab\n" +
                "• SampleScene: GameplayNetworkSpawner + WinController\n" +
                "• NetworkManager: prefab list + Connection Approval\n\n" +
                "Bitiş: Map → Create Finish Zone\n\n" +
                "Önce Phase 3/4/5, Map spawn noktaları kurulu olsun.\n" +
                "Test: 2 pencere host+client → BAŞLAT.",
                "OK");
        }

        private static GameObject CreateOrUpdateNetworkTravelerPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                ConfigureNetworkTravelerPrefab(existing);
                return existing;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "NetworkTraveler";
            go.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
                Object.DestroyImmediate(rb);

            var agent = go.AddComponent<NavMeshAgent>();
            agent.height = 2f;
            agent.radius = 0.35f;
            agent.speed = 5f;
            agent.angularSpeed = 720f;

            go.AddComponent<TravelerMovementController>();
            go.AddComponent<TravelerTouchInput>();
            go.AddComponent<TravelerTeamMarker>();
            go.AddComponent<NetworkTravelerController>();
            go.AddComponent<NetworkPlayerIdentity>();

            var netObj = go.AddComponent<NetworkObject>();
            var nt = go.AddComponent<NetworkTransform>();
            nt.SyncPositionX = nt.SyncPositionY = nt.SyncPositionZ = true;
            ConfigureNetworkTravelerTransform(nt);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static NetworkPrefabsList CreateOrUpdatePrefabList(GameObject prefab)
        {
            var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(PrefabListPath);
            if (list == null)
            {
                list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
                AssetDatabase.CreateAsset(list, PrefabListPath);
            }

            var existing = new System.Collections.Generic.List<NetworkPrefab>(list.PrefabList);
            foreach (var entry in existing)
                list.Remove(entry);

            list.Add(new NetworkPrefab { Prefab = prefab });
            EditorUtility.SetDirty(list);
            return list;
        }

        private static void ConfigureGameplayScene(GameObject travelerPrefab)
        {
            if (!System.IO.File.Exists(GameplayScenePath))
                return;

            var active = SceneManager.GetActiveScene();
            EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

            var root = GameObject.Find("_CoopPuzzle_Gameplay");
            if (root == null)
            {
                Debug.LogWarning("Phase 6: _CoopPuzzle_Gameplay yok. Önce Phase 3 çalıştır.");
                return;
            }

            var spawnerGo = GameObject.Find("GameplayNetworkSpawner");
            if (spawnerGo == null)
            {
                spawnerGo = new GameObject("GameplayNetworkSpawner");
                Undo.RegisterCreatedObjectUndo(spawnerGo, "Create spawner");
                spawnerGo.transform.SetParent(root.transform, false);
            }

            var spawner = spawnerGo.GetComponent<GameplayNetworkSpawner>();
            if (spawner == null)
                spawner = Undo.AddComponent<GameplayNetworkSpawner>(spawnerGo);

            EnsureDoorNetworkBridge(spawnerGo);
            EnsureWinController(spawnerGo);
            EnsureGameplayWinUi(root);

            var so = new SerializedObject(spawner);
            so.FindProperty("networkTravelerPrefab").objectReferenceValue = travelerPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsureTravelerSpawnPoints();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            if (!string.IsNullOrEmpty(active.path) && active.path != GameplayScenePath)
                EditorSceneManager.OpenScene(active.path, OpenSceneMode.Single);
        }

        private static void ConfigureLobbyNetworkManager(NetworkPrefabsList prefabList)
        {
            if (!System.IO.File.Exists(LobbyScenePath))
                return;

            var active = SceneManager.GetActiveScene();
            EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);

            var networkManager = Object.FindAnyObjectByType<NetworkManager>();
            if (networkManager == null)
                return;

            var so = new SerializedObject(networkManager);
            var configProp = so.FindProperty("NetworkConfig");
            if (configProp != null)
            {
                var enableScenes = configProp.FindPropertyRelative("EnableSceneManagement");
                if (enableScenes != null)
                    enableScenes.boolValue = true;

                var approval = configProp.FindPropertyRelative("ConnectionApproval");
                if (approval != null)
                    approval.boolValue = true;

                var autoSpawn = configProp.FindPropertyRelative("AutoSpawnPlayerPrefabClientSide");
                if (autoSpawn != null)
                    autoSpawn.boolValue = false;

                var prefabs = configProp.FindPropertyRelative("Prefabs");
                AssignNetworkPrefabsList(prefabs, prefabList);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(networkManager);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            if (!string.IsNullOrEmpty(active.path) && active.path != LobbyScenePath)
                EditorSceneManager.OpenScene(active.path, OpenSceneMode.Single);
        }

        /// <summary>NGO 2.11: Prefabs is NetworkPrefabs; list lives under NetworkPrefabsLists[].</summary>
        private static void AssignNetworkPrefabsList(SerializedProperty prefabsRoot, NetworkPrefabsList prefabList)
        {
            if (prefabsRoot == null || prefabList == null)
                return;

            var lists = prefabsRoot.FindPropertyRelative("NetworkPrefabsLists");
            if (lists == null || !lists.isArray)
            {
                Debug.LogWarning("Phase 6: NetworkPrefabsLists bulunamadı; prefab listesi atlanmadı.");
                return;
            }

            for (int i = 0; i < lists.arraySize; i++)
            {
                if (lists.GetArrayElementAtIndex(i).objectReferenceValue == prefabList)
                    return;
            }

            lists.InsertArrayElementAtIndex(lists.arraySize);
            lists.GetArrayElementAtIndex(lists.arraySize - 1).objectReferenceValue = prefabList;
        }

        private static void ConfigureNetworkTravelerPrefab(GameObject prefabRoot)
        {
            var nt = prefabRoot.GetComponent<NetworkTransform>();
            if (nt == null)
                return;

            ConfigureNetworkTravelerTransform(nt);
            EditorUtility.SetDirty(prefabRoot);
        }

        private static void ConfigureNetworkTravelerTransform(NetworkTransform nt)
        {
            var so = new SerializedObject(nt);
            var authority = so.FindProperty("AuthorityMode");
            if (authority != null)
                authority.enumValueIndex = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("Tools/CoopPuzzle/Setup/Fix NetworkTraveler Owner Authority")]
        public static void FixNetworkTravelerOwnerAuthority()
        {
            if (CoopPuzzleEditorPlayModeGuard.BlockIfPlaying("Fix NetworkTraveler"))
                return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("CoopPuzzle", "NetworkTraveler prefab bulunamadı.", "OK");
                return;
            }

            ConfigureNetworkTravelerPrefab(prefab);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("CoopPuzzle", "NetworkTransform → Owner authority ayarlandı.", "OK");
        }

        private static void EnsureDoorNetworkBridge(GameObject host)
        {
            if (host.GetComponent<NetworkObject>() == null)
                Undo.AddComponent<NetworkObject>(host);

            if (host.GetComponent<DoorGameplayNetworkBridge>() == null)
                Undo.AddComponent<DoorGameplayNetworkBridge>(host);
        }

        private static void EnsureWinController(GameObject host)
        {
            if (host.GetComponent<NetworkObject>() == null)
                Undo.AddComponent<NetworkObject>(host);

            if (host.GetComponent<GameplayWinController>() == null)
                Undo.AddComponent<GameplayWinController>(host);
        }

        private static void EnsureGameplayWinUi(GameObject gameplayRoot)
        {
            if (Object.FindAnyObjectByType<GameplayWinUI>() != null)
                return;

            var uiGo = new GameObject("GameplayWinUI");
            Undo.RegisterCreatedObjectUndo(uiGo, "Win UI");
            uiGo.transform.SetParent(gameplayRoot.transform, false);
            Undo.AddComponent<GameplayWinUI>(uiGo);
        }

        private static void EnsureTravelerSpawnPoints()
        {
            CoopPuzzleMapSetup.CreateTeam1TravelerSpawn();
            CoopPuzzleMapSetup.CreateTeam2TravelerSpawn();
        }

        [MenuItem("Tools/CoopPuzzle/Map/Remove Duplicate Traveler Spawns")]
        public static void RemoveDuplicateTravelerSpawns()
        {
            if (CoopPuzzleEditorPlayModeGuard.BlockIfPlaying("Remove duplicate spawns"))
                return;

            var kept = 0;
            var removed = 0;
            foreach (SpawnTeam team in System.Enum.GetValues(typeof(SpawnTeam)))
            {
                GameplaySpawnPoint first = null;
                foreach (var sp in Object.FindObjectsByType<GameplaySpawnPoint>(FindObjectsInactive.Include))
                {
                    if (sp.Team != team)
                        continue;

                    if (first == null)
                    {
                        first = sp;
                        kept++;
                        continue;
                    }

                    Undo.DestroyObjectImmediate(sp.gameObject);
                    removed++;
                }
            }

            EditorUtility.DisplayDialog("CoopPuzzle", $"Duplicate spawn temizlendi.\nKalan: {kept}\nSilinen: {removed}", "OK");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parts = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
