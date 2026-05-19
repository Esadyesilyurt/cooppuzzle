using CoopPuzzle.Gameplay.Doors;
using CoopPuzzle.Gameplay.Map;
using CoopPuzzle.Gameplay.Player;
using CoopPuzzle.Questions;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace CoopPuzzle.EditorTools
{
    /// <summary>
    /// kürdistanprojee.fbx haritasını sahneye ekler; spawn ve kapı yerleştirme yardımcıları.
    /// menü.unity lobby UI'ye dokunmaz.
    /// </summary>
    public static class CoopPuzzleMapSetup
    {
        private const string MapFbxPath = "Assets/kürdistanprojee.fbx";
        private const string MapFbxFileName = "kürdistanprojee";
        private const string TargetScenePath = "Assets/Scenes/SampleScene.unity";
        private const string GameplayRootName = "_CoopPuzzle_Gameplay";
        private const string MapChildName = "Map_KurdistanProje";
        private static readonly string[] LegacyMapChildNames = { "Map_mapp2", "Map_KurdistanProje" };
        private const string MarkersName = "Markers";
        private const string SpawnFolderName = "SpawnPoints";
        private const string DoorsFolderName = "Doors";
        private const string GroundChildName = "MapWalkableGround";

        [MenuItem("Tools/CoopPuzzle/Map/Add Walkable Ground Under Map", false, 11)]
        public static void AddWalkableGroundMenu()
        {
            EnsureSceneOpen();
            var map = ResolveMapRoot();
            if (map == null)
            {
                EditorUtility.DisplayDialog(
                    "CoopPuzzle",
                    "Harita bulunamadı.\n\n" +
                    "Hierarchy'de haritayı seç veya önce Import Map çalıştır.",
                    "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Add map ground");
            var group = Undo.GetCurrentGroup();

            var ground = CreateOrRefreshWalkableGround(map);
            var gameplayRoot = map.transform.parent != null ? map.transform.parent.gameObject : EnsureGameplayRoot();
            BakeNavMesh(gameplayRoot);
            SnapTravelerToSpawn(gameplayRoot.transform);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(group);

            Selection.activeGameObject = ground;
            EditorUtility.DisplayDialog(
                "CoopPuzzle — Zemin",
                "Yürünebilir zemin haritanın ALTINA yatay eklendi (MapWalkableGround).\n\n" +
                "• Dünya XZ düzleminde (harita döndürülmüş olsa bile)\n" +
                "• Eski dik zemin varsa silindi, yenisi oluşturuldu\n" +
                "• NavMesh yeniden bake edildi\n\n" +
                "Sonra kapıları yerleştirebilirsin.",
                "OK");
        }

        [MenuItem("Tools/CoopPuzzle/Map/Rebake NavMesh", false, 12)]
        public static void RebakeNavMeshMenu()
        {
            EnsureSceneOpen();
            var root = EnsureGameplayRoot();
            BakeNavMesh(root);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("CoopPuzzle", "NavMesh yeniden bake edildi.", "OK");
        }

        [MenuItem("Tools/CoopPuzzle/Map/Import Map (kürdistanprojee — One Click)", false, 10)]
        public static void ImportMapOneClick() => ImportMapInternal();

        [MenuItem("Tools/CoopPuzzle/Map/Import mapp2 (One Click)", false, 11)]
        public static void ImportLegacyMapp2()
        {
            EditorUtility.DisplayDialog(
                "CoopPuzzle",
                "Eski harita: mapp2.fbx\n\n" +
                "Yeni harita: kürdistanprojee.fbx\n" +
                "Lütfen: Import Map (kürdistanprojee — One Click) kullan.",
                "OK");
        }

        private static void ImportMapInternal()
        {
            if (CoopPuzzleEditorPlayModeGuard.BlockIfPlaying("Harita import"))
                return;

            if (!System.IO.File.Exists(MapFbxPath))
            {
                EditorUtility.DisplayDialog("CoopPuzzle", $"Harita bulunamadı:\n{MapFbxPath}", "OK");
                return;
            }

            EnsureSceneOpen();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Import map");
            var group = Undo.GetCurrentGroup();

            var gameplayRoot = EnsureGameplayRoot();
            RemoveArenaFloor(gameplayRoot);
            RemoveExistingMapInstances(gameplayRoot.transform);
            var mapInstance = InstantiateMapUnder(gameplayRoot.transform);
            SetupMapPhysicsAndNav(mapInstance);
            EnsureMarkerFolders(gameplayRoot.transform);
            BakeNavMesh(gameplayRoot);
            SnapTravelerToSpawn(gameplayRoot.transform);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(group);

            Selection.activeGameObject = mapInstance;
            EditorUtility.DisplayDialog(
                "CoopPuzzle — Harita",
                "kürdistanprojee.fbx sahneye eklendi.\n\n" +
                "Sıradaki adımlar:\n" +
                "1) Haritanın konum/ölçek/yönünü ayarla\n" +
                "2) Tools → Map → Add Walkable Ground Under Map\n" +
                "3) Spawn ve kapıları yerleştir\n" +
                "4) Sahneyi kaydet, Play ile dene\n\n" +
                "Detaylı adımlar: Show Placement Guide",
                "OK");
        }

        [MenuItem("Tools/CoopPuzzle/Map/Show Placement Guide")]
        public static void ShowPlacementGuide()
        {
            EditorUtility.DisplayDialog(
                "Spawn & Kapı Yerleştirme",
                "ZEMİN (haritada zemin yoksa)\n" +
                "• Haritayı konumlandırdıktan sonra:\n" +
                "  Tools → CoopPuzzle → Map → Add Walkable Ground Under Map\n" +
                "• Harita altında MapWalkableGround oluşur\n" +
                "• Yeşil gizmo ile boyutu kontrol et; gerekirse Scale/Position düzelt\n\n" +
                "SPAWN NOKTASI\n" +
                "• Scene görünümünde doğru yere git\n" +
                "• Tools → CoopPuzzle → Map → Create Team1/2 Traveler Spawn\n" +
                "• Oluşan objeyi sürükleyerek konumlandır (mavi = Gezgin)\n" +
                "• Bilge spawn YOK — Gezgin'i izler, belge UI (Phase 4)\n\n" +
                "BİTİŞ ALANI\n" +
                "• Hedef noktaya git → Create Finish Zone\n" +
                "• Sarı gizmo ile alanı ayarla (BoxCollider)\n" +
                "• İlk giren takımın Gezgini kazanır\n\n" +
                "KAPI\n" +
                "• Kapı geçidinin önüne Scene görünümünde git\n" +
                "• Tools → CoopPuzzle → Map → Create Door At Scene View\n" +
                "• Door_X objesini geçide hizala; Leaf küpü kapı kanadı\n" +
                "• DoorInteractable → Interact Distance ayarla\n" +
                "• QuestionManager door listesine slot ekle (Phase 2/3 kurulumu varsa otomatik)\n\n" +
                "NAVMESH\n" +
                "• Harita import sonrası otomatik bake edilir\n" +
                "• Yürünemez alan varsa Map altındaki mesh'lere collider kontrol et",
                "OK");
        }

        [MenuItem("Tools/CoopPuzzle/Map/Create Team1 Traveler Spawn", false, 20)]
        public static void CreateTeam1TravelerSpawn() => CreateTravelerSpawn(SpawnTeam.Team1);

        [MenuItem("Tools/CoopPuzzle/Map/Create Team2 Traveler Spawn", false, 21)]
        public static void CreateTeam2TravelerSpawn() => CreateTravelerSpawn(SpawnTeam.Team2);

        [MenuItem("Tools/CoopPuzzle/Map/Create Finish Zone", false, 23)]
        public static void CreateFinishZone()
        {
            if (CoopPuzzleEditorPlayModeGuard.BlockIfPlaying("Create Finish Zone"))
                return;

            EnsureSceneOpen();

            var existing = Object.FindAnyObjectByType<GameplayFinishZone>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[MapSetup] FinishZone zaten var.");
                return;
            }

            var gameplayRoot = EnsureGameplayRoot();
            var markers = EnsureMarkerFolders(gameplayRoot.transform);
            var pos = GetSceneViewPlacementPosition();

            var go = new GameObject("FinishZone");
            Undo.RegisterCreatedObjectUndo(go, "Create finish zone");
            go.transform.SetParent(markers.spawns.parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;

            var box = Undo.AddComponent<BoxCollider>(go);
            box.isTrigger = true;
            box.size = new Vector3(5f, 3f, 5f);
            box.center = new Vector3(0f, 1.5f, 0f);

            var rb = Undo.AddComponent<Rigidbody>(go);
            rb.isKinematic = true;
            rb.useGravity = false;

            Undo.AddComponent<GameplayFinishZone>(go);

            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("Tools/CoopPuzzle/Map/Snap All Spawns To NavMesh", false, 22)]
        public static void SnapAllSpawnsToNavMesh()
        {
            if (CoopPuzzleEditorPlayModeGuard.BlockIfPlaying("Snap spawns"))
                return;

            var snapped = 0;
            var failed = new System.Collections.Generic.List<string>();
            foreach (var sp in Object.FindObjectsByType<GameplaySpawnPoint>(FindObjectsInactive.Include))
            {
                Undo.RecordObject(sp, "Bake spawn");
                sp.BakeSpawnPosition();

                if (GameplaySpawnService.IsMarkerOnNavMesh(sp.GetSpawnPosition(), 0.35f))
                    snapped++;
                else
                    failed.Add(sp.name);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            var msg = $"Bake edilen spawn: {snapped}";
            if (failed.Count > 0)
                msg += $"\n\nUyarı — marker'ı Scene'de yeşil NavMesh üstüne taşı, tekrar Snap:\n• {string.Join("\n• ", failed)}";

            EditorUtility.DisplayDialog("CoopPuzzle", msg, "OK");
        }

        [MenuItem("Tools/CoopPuzzle/Map/Remove Obsolete Sage Spawn Markers", false, 22)]
        public static void RemoveObsoleteSageSpawns()
        {
            EnsureSceneOpen();
            var removed = 0;
            foreach (var sp in Object.FindObjectsByType<GameplaySpawnPoint>(FindObjectsInactive.Include))
            {
                if (!sp.name.Contains("Sage")) continue;
                Undo.DestroyObjectImmediate(sp.gameObject);
                removed++;
            }

            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t == null || !t.name.Contains("Spawn_") || !t.name.Contains("Sage")) continue;
                if (t.GetComponent<GameplaySpawnPoint>() != null) continue;
                Undo.DestroyObjectImmediate(t.gameObject);
                removed++;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("CoopPuzzle", $"Kaldırılan eski Bilge spawn işaretçisi: {removed}", "OK");
        }

        [MenuItem("Tools/CoopPuzzle/Map/Create Door At Scene View", false, 40)]
        public static void CreateDoorAtSceneView()
        {
            EnsureSceneOpen();
            var gameplayRoot = EnsureGameplayRoot();
            var doorsFolder = EnsureMarkerFolders(gameplayRoot.transform).doors;
            var pos = GetSceneViewPlacementPosition();
            var index = doorsFolder.childCount + 1;
            CreateDoor(doorsFolder, pos, $"Door_{index}");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static void CreateTravelerSpawn(SpawnTeam team)
        {
            EnsureSceneOpen();
            var existing = FindTravelerSpawn(team);
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log($"[MapSetup] {team} spawn zaten var: {existing.name}");
                return;
            }

            var gameplayRoot = EnsureGameplayRoot();
            var folders = EnsureMarkerFolders(gameplayRoot.transform);
            var pos = GetSceneViewPlacementPosition();

            var go = new GameObject($"Spawn_{team}_Traveler");
            Undo.RegisterCreatedObjectUndo(go, "Create traveler spawn");
            go.transform.SetParent(folders.spawns, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;

            var sp = Undo.AddComponent<GameplaySpawnPoint>(go);
            sp.Configure(team);
            sp.BakeSpawnPosition();

            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static GameObject CreateDoor(Transform parent, Vector3 worldPos, string doorName)
        {
            var doorRoot = new GameObject(doorName);
            Undo.RegisterCreatedObjectUndo(doorRoot, "Door root");
            doorRoot.transform.SetParent(parent, false);
            doorRoot.transform.position = worldPos;

            var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(leaf, "Door leaf");
            leaf.name = "Leaf";
            leaf.transform.SetParent(doorRoot.transform, false);
            leaf.transform.localPosition = Vector3.zero;
            leaf.transform.localScale = new Vector3(0.35f, 2.2f, 1.6f);

            var lrb = leaf.GetComponent<Rigidbody>();
            if (lrb != null) Undo.DestroyObjectImmediate(lrb);

            var col = leaf.GetComponent<BoxCollider>();
            if (col != null)
                col.isTrigger = false;

            CoopPuzzleDoorBlockingFix.EnsureDoorBlocker(doorRoot);

            Undo.AddComponent<DoorQuestionSlot>(doorRoot);
            var door = Undo.AddComponent<DoorInteractable>(doorRoot);

            var blocker = doorRoot.transform.Find("DoorBlocker");
            var blockerCol = blocker != null ? blocker.GetComponent<Collider>() : null;
            var obstacle = doorRoot.GetComponent<NavMeshObstacle>();

            var dso = new SerializedObject(door);
            dso.FindProperty("questionSlot").objectReferenceValue = doorRoot.GetComponent<DoorQuestionSlot>();
            dso.FindProperty("blockingCollider").objectReferenceValue = blockerCol;
            dso.FindProperty("navMeshObstacle").objectReferenceValue = obstacle;
            dso.FindProperty("doorLeaf").objectReferenceValue = leaf.transform;
            dso.FindProperty("interactDistance").floatValue = 3f;
            dso.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = doorRoot;
            return doorRoot;
        }

        private static Vector3 GetSceneViewPlacementPosition()
        {
            if (Selection.activeTransform != null)
                return Selection.activeTransform.position;

            if (SceneView.lastActiveSceneView != null)
                return SceneView.lastActiveSceneView.pivot;

            return Vector3.zero;
        }

        private static void EnsureSceneOpen()
        {
            if (CoopPuzzleEditorPlayModeGuard.BlockIfPlaying("Sahne düzenleme"))
                return;

            if (!System.IO.File.Exists(TargetScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, TargetScenePath);
            }

            if (EditorSceneManager.GetActiveScene().path != TargetScenePath)
                EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        }

        private static GameObject EnsureGameplayRoot()
        {
            var existing = GameObject.Find(GameplayRootName);
            if (existing != null)
                return existing;

            var root = new GameObject(GameplayRootName);
            Undo.RegisterCreatedObjectUndo(root, "Gameplay root");
            return root;
        }

        private static void RemoveExistingMapInstances(Transform gameplayRoot)
        {
            foreach (var mapName in LegacyMapChildNames)
            {
                var old = gameplayRoot.Find(mapName);
                if (old != null)
                    Undo.DestroyObjectImmediate(old.gameObject);
            }
        }

        private static GameObject InstantiateMapUnder(Transform gameplayRoot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapFbxPath);
            if (prefab == null)
            {
                Debug.LogError($"Harita yüklenemedi: {MapFbxPath}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Map instance");
            instance.name = MapChildName;
            instance.transform.SetParent(gameplayRoot, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private static void SetupMapPhysicsAndNav(GameObject mapRoot)
        {
            if (mapRoot == null) return;

            var meshFilters = mapRoot.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;

                var go = mf.gameObject;
                var mc = go.GetComponent<MeshCollider>();
                if (mc == null)
                    mc = Undo.AddComponent<MeshCollider>(go);

                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;

                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.NavigationStatic);
            }
        }

        private static void RemoveArenaFloor(GameObject gameplayRoot)
        {
            var floor = gameplayRoot.transform.Find("ArenaFloor");
            if (floor != null)
                Undo.DestroyObjectImmediate(floor.gameObject);
        }

        private static (Transform spawns, Transform doors) EnsureMarkerFolders(Transform gameplayRoot)
        {
            var markers = gameplayRoot.Find(MarkersName);
            if (markers == null)
            {
                var markersGo = new GameObject(MarkersName);
                Undo.RegisterCreatedObjectUndo(markersGo, "Markers");
                markersGo.transform.SetParent(gameplayRoot, false);
                markers = markersGo.transform;
            }

            var spawns = markers.Find(SpawnFolderName);
            if (spawns == null)
            {
                var spawnsGo = new GameObject(SpawnFolderName);
                Undo.RegisterCreatedObjectUndo(spawnsGo, "SpawnPoints folder");
                spawnsGo.transform.SetParent(markers, false);
                spawns = spawnsGo.transform;
            }

            var doors = markers.Find(DoorsFolderName);
            if (doors == null)
            {
                var doorsGo = new GameObject(DoorsFolderName);
                Undo.RegisterCreatedObjectUndo(doorsGo, "Doors folder");
                doorsGo.transform.SetParent(markers, false);
                doors = doorsGo.transform;
            }

            return (spawns, doors);
        }

        private static void BakeNavMesh(GameObject gameplayRoot)
        {
            var surface = gameplayRoot.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = Undo.AddComponent<NavMeshSurface>(gameplayRoot);
                surface.collectObjects = CollectObjects.Children;
            }

            surface.BuildNavMesh();
        }

        private static void SnapTravelerToSpawn(Transform gameplayRoot)
        {
            var traveler = gameplayRoot.GetComponentInChildren<TravelerMovementController>(true);
            if (traveler == null) return;

            var spawn = FindTravelerSpawn(SpawnTeam.Team1);
            if (spawn == null) return;

            var agent = traveler.GetComponent<NavMeshAgent>();
            var pos = spawn.transform.position;
            if (agent != null && NavMesh.SamplePosition(pos, out var hit, 4f, NavMesh.AllAreas))
                traveler.transform.position = hit.position;
            else
                traveler.transform.position = pos + Vector3.up;
        }

        private static GameplaySpawnPoint FindTravelerSpawn(SpawnTeam team)
        {
            foreach (var sp in Object.FindObjectsByType<GameplaySpawnPoint>(FindObjectsInactive.Exclude))
            {
                if (sp.Team == team)
                    return sp;
            }

            return null;
        }

        private static GameObject ResolveMapRoot()
        {
            if (Selection.activeTransform != null)
            {
                var t = Selection.activeTransform;
                while (t != null)
                {
                    if (IsMapHierarchyNode(t.name))
                        return t.gameObject;
                    t = t.parent;
                }
            }

            var gameplay = GameObject.Find(GameplayRootName);
            if (gameplay != null)
            {
                var map = gameplay.transform.Find(MapChildName);
                if (map != null)
                    return map.gameObject;
            }

            foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            {
                if (IsMapHierarchyNode(go.name))
                    return go.gameObject;
            }

            return null;
        }

        private static GameObject CreateOrRefreshWalkableGround(GameObject mapRoot)
        {
            const float defaultPadding = 2f;
            const float defaultThickness = 0.25f;
            const float defaultBelow = 0.05f;

            var existing = mapRoot.transform.Find(GroundChildName);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);

            if (!TryGetWorldMeshBounds(mapRoot.transform, out var worldBounds))
            {
                Debug.LogWarning("Harita mesh sınırı bulunamadı; varsayılan 40x40 zemin oluşturuluyor.");
                worldBounds = new Bounds(mapRoot.transform.position, new Vector3(40f, 4f, 40f));
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(ground, "Map ground");
            ground.name = GroundChildName;

            var rb = ground.GetComponent<Rigidbody>();
            if (rb != null) Undo.DestroyObjectImmediate(rb);

            var marker = Undo.AddComponent<MapWalkableGround>(ground);

            var thickness = defaultThickness;
            var padding = defaultPadding;
            var below = defaultBelow;

            if (!TryBuildHorizontalGroundTransform(worldBounds, thickness, padding, below,
                    out var worldPosition, out var worldRotation, out var worldScale))
            {
                worldPosition = worldBounds.center;
                worldRotation = Quaternion.identity;
                worldScale = new Vector3(40f, thickness, 40f);
            }

            // Dünya uzayında yatay zemin; harita döndürülmüş olsa bile XZ düzleminde kalır.
            ground.transform.SetPositionAndRotation(worldPosition, worldRotation);
            ground.transform.localScale = worldScale;
            ground.transform.SetParent(mapRoot.transform, true);

            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.NavigationStatic);

            var renderer = ground.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null)
                    mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.35f, 0.38f, 0.42f, 1f);
                renderer.sharedMaterial = mat;
            }

            return ground;
        }

        /// <summary>
        /// En ince eksen = haritanın "yüksekliği"; zemin ona dik yatay düzlemde oluşturulur.
        /// </summary>
        private static bool TryBuildHorizontalGroundTransform(
            Bounds worldBounds,
            float thickness,
            float padding,
            float below,
            out Vector3 worldPosition,
            out Quaternion worldRotation,
            out Vector3 worldScale)
        {
            worldPosition = default;
            worldRotation = Quaternion.identity;
            worldScale = default;

            var size = worldBounds.size;
            if (size.sqrMagnitude < 0.01f)
                return false;

            int upAxis;
            if (size.y <= size.x && size.y <= size.z)
                upAxis = 1;
            else if (size.x <= size.y && size.x <= size.z)
                upAxis = 0;
            else
                upAxis = 2;

            float footprintA;
            float footprintB;
            Vector3 bottomCenter;

            switch (upAxis)
            {
                case 1:
                    footprintA = size.x;
                    footprintB = size.z;
                    bottomCenter = new Vector3(worldBounds.center.x, worldBounds.min.y, worldBounds.center.z);
                    worldRotation = Quaternion.identity;
                    worldScale = new Vector3(
                        Mathf.Max(footprintA + padding * 2f, 4f),
                        thickness,
                        Mathf.Max(footprintB + padding * 2f, 4f));
                    break;
                case 0:
                    footprintA = size.y;
                    footprintB = size.z;
                    bottomCenter = new Vector3(worldBounds.min.x, worldBounds.center.y, worldBounds.center.z);
                    worldRotation = Quaternion.Euler(0f, 0f, 90f);
                    worldScale = new Vector3(
                        Mathf.Max(footprintA + padding * 2f, 4f),
                        thickness,
                        Mathf.Max(footprintB + padding * 2f, 4f));
                    break;
                default:
                    footprintA = size.x;
                    footprintB = size.y;
                    bottomCenter = new Vector3(worldBounds.center.x, worldBounds.center.y, worldBounds.min.z);
                    worldRotation = Quaternion.Euler(90f, 0f, 0f);
                    worldScale = new Vector3(
                        Mathf.Max(footprintA + padding * 2f, 4f),
                        thickness,
                        Mathf.Max(footprintB + padding * 2f, 4f));
                    break;
            }

            var offset = GetAxisVector(upAxis) * (below + thickness * 0.5f);
            worldPosition = bottomCenter - offset;
            return true;
        }

        private static Vector3 GetAxisVector(int axis) =>
            axis switch
            {
                0 => Vector3.right,
                1 => Vector3.up,
                _ => Vector3.forward
            };

        private static bool TryGetWorldMeshBounds(Transform mapRoot, out Bounds worldBounds)
        {
            worldBounds = default;
            var initialized = false;

            foreach (var r in mapRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (r.GetComponent<MapWalkableGround>() != null)
                    continue;

                if (!initialized)
                {
                    worldBounds = r.bounds;
                    initialized = true;
                }
                else
                {
                    worldBounds.Encapsulate(r.bounds);
                }
            }

            return initialized && worldBounds.size.sqrMagnitude > 0.01f;
        }

        private static bool IsMapHierarchyNode(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return false;

            if (objectName == MapChildName)
                return true;

            foreach (var legacy in LegacyMapChildNames)
            {
                if (objectName == legacy)
                    return true;
            }

            return objectName.Contains(MapFbxFileName, System.StringComparison.OrdinalIgnoreCase)
                   || objectName.Contains("mapp2", System.StringComparison.OrdinalIgnoreCase);
        }

        private static Vector3[] GetWorldBoundsCorners(Bounds b)
        {
            var c = b.center;
            var e = b.extents;
            return new[]
            {
                c + new Vector3(e.x, e.y, e.z),
                c + new Vector3(e.x, e.y, -e.z),
                c + new Vector3(e.x, -e.y, e.z),
                c + new Vector3(e.x, -e.y, -e.z),
                c + new Vector3(-e.x, e.y, e.z),
                c + new Vector3(-e.x, e.y, -e.z),
                c + new Vector3(-e.x, -e.y, e.z),
                c + new Vector3(-e.x, -e.y, -e.z)
            };
        }
    }
}
