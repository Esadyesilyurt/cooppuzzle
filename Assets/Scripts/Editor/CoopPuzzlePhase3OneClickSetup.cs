using TopDownCameraFollow = CoopPuzzle.Gameplay.Camera.TopDownCameraFollow;
using CoopPuzzle.Gameplay.Doors;
using CoopPuzzle.Gameplay.Player;
using CoopPuzzle.Gameplay.Questions;
using CoopPuzzle.Gameplay.UI;
using CoopPuzzle.Questions;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoopPuzzle.EditorTools
{
  /// <summary>
  /// Lobby/menü UI'ye dokunmaz. SampleScene'e minimal test alanı + Gezgin mekanikleri ekler (labirent değil).
  /// </summary>
  public static class CoopPuzzlePhase3OneClickSetup
  {
    private const string TargetScenePath = "Assets/Scenes/SampleScene.unity";
    private const string RootName = "_CoopPuzzle_Gameplay";

    [MenuItem("Tools/CoopPuzzle/Setup/Apply Tighter Gameplay Camera")]
    public static void ApplyTighterGameplayCamera()
    {
      var follow = Object.FindAnyObjectByType<TopDownCameraFollow>();
      if (follow == null)
      {
        EditorUtility.DisplayDialog("CoopPuzzle", "TopDownCameraFollow bulunamadı. Önce Phase 3 kurulumunu çalıştır.", "OK");
        return;
      }

      Undo.RecordObject(follow, "Tighter camera");
      var so = new SerializedObject(follow);
      so.FindProperty("offset").vector3Value = new Vector3(0f, 10f, -5.5f);
      so.FindProperty("orthographicSize").floatValue = 7.5f;
      so.FindProperty("followSmooth").floatValue = 10f;
      so.ApplyModifiedProperties();

      var cam = follow.GetComponent<UnityEngine.Camera>();
      if (cam != null)
      {
        Undo.RecordObject(cam, "Tighter camera size");
        cam.orthographicSize = 7.5f;
      }

      follow.ApplyOrthographicSize();
      EditorUtility.DisplayDialog("CoopPuzzle", "Kamera yakınlaştırıldı (Size 7.5, daha düşük açı).", "OK");
    }

    [MenuItem("Tools/CoopPuzzle/Setup/Phase 3 Setup (One Click)")]
    public static void SetupPhase3()
    {
      if (CoopPuzzleEditorPlayModeGuard.BlockIfPlaying("Phase 3 Setup"))
        return;

      EnsureFolder("Assets/Scenes");
      EnsureFolder("Assets/Prefabs");

      if (!System.IO.File.Exists(TargetScenePath))
      {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, TargetScenePath);
      }

      EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

      Undo.IncrementCurrentGroup();
      Undo.SetCurrentGroupName("Phase 3 Gameplay Setup");
      int group = Undo.GetCurrentGroup();

      RemoveIfExists(RootName);

      var root = new GameObject(RootName);
      Undo.RegisterCreatedObjectUndo(root, "Create gameplay root");

      BuildMinimalArena(root.transform);
      var player = BuildPlayer(root.transform);
      var cam = BuildCamera(player.transform);
      var doors = BuildDoors(root.transform);
      var ui = BuildQuestionUi(root.transform);
      EnsureEventSystem();
      var flow = Undo.AddComponent<QuestionFlowController>(root);

      EnsureQuestionManager(root.transform, doors);
      BakeNavMesh(root);

      WireFlow(flow, ui, player, cam);

      EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
      Undo.CollapseUndoOperations(group);

      EditorUtility.DisplayDialog(
        "CoopPuzzle",
        "Phase 3 (Gezgin) kuruldu.\n\n" +
        "- Minimal test alanı (labirent değil)\n" +
        "- Point & click + NavMesh\n" +
        "- Kapı etkileşimi + soru paneli\n\n" +
        "SampleScene'i kaydet ve Play ile dene.",
        "OK");
    }

    private static void BuildMinimalArena(Transform root)
    {
      var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
      Undo.RegisterCreatedObjectUndo(floor, "Floor");
      floor.name = "ArenaFloor";
      floor.transform.SetParent(root, false);
      floor.transform.position = new Vector3(10f, -0.25f, 10f);
      floor.transform.localScale = new Vector3(22f, 0.5f, 22f);

      var rb = floor.GetComponent<Rigidbody>();
      if (rb != null) Undo.DestroyObjectImmediate(rb);

      var light = new GameObject("Directional Light");
      Undo.RegisterCreatedObjectUndo(light, "Light");
      light.transform.SetParent(root, false);
      light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
      var l = Undo.AddComponent<Light>(light);
      l.type = LightType.Directional;
    }

    private static GameObject BuildPlayer(Transform root)
    {
      var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
      Undo.RegisterCreatedObjectUndo(go, "Player");
      go.name = "Traveler";
      go.transform.SetParent(root, false);
      go.transform.position = new Vector3(10f, 1f, 8f);
      go.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

      var rb = go.GetComponent<Rigidbody>();
      if (rb != null) Undo.DestroyObjectImmediate(rb);

      var agent = Undo.AddComponent<NavMeshAgent>(go);
      agent.height = 2f;
      agent.radius = 0.35f;
      agent.speed = 5f;
      agent.angularSpeed = 720f;

      Undo.AddComponent<TravelerMovementController>(go);
      Undo.AddComponent<TravelerTouchInput>(go);
      return go;
    }

    private static UnityEngine.Camera BuildCamera(Transform player)
    {
      var camGo = new GameObject("GameplayCamera");
      Undo.RegisterCreatedObjectUndo(camGo, "Camera");
      camGo.transform.position = player.position + new Vector3(0f, 10f, -6f);
      camGo.transform.rotation = Quaternion.Euler(58f, 0f, 0f);

      var cam = Undo.AddComponent<UnityEngine.Camera>(camGo);
      cam.orthographic = true;
      cam.orthographicSize = 7.5f;
      cam.tag = "MainCamera";

      var follow = Undo.AddComponent<TopDownCameraFollow>(camGo);
      var so = new SerializedObject(follow);
      so.FindProperty("target").objectReferenceValue = player;
      so.FindProperty("viewCamera").objectReferenceValue = cam;
      so.FindProperty("offset").vector3Value = new Vector3(0f, 10f, -5.5f);
      so.FindProperty("orthographicSize").floatValue = 7.5f;
      so.FindProperty("followSmooth").floatValue = 10f;
      so.ApplyModifiedPropertiesWithoutUndo();

      return cam;
    }

    private static DoorInteractable[] BuildDoors(Transform root)
    {
      var parent = new GameObject("Doors");
      Undo.RegisterCreatedObjectUndo(parent, "Doors parent");
      parent.transform.SetParent(root, false);

      var positions = new[]
      {
        new Vector3(10f, 1f, 14f),
        new Vector3(10f, 1f, 6f),
      };

      var doors = new DoorInteractable[positions.Length];
      for (int i = 0; i < positions.Length; i++)
      {
        var doorRoot = new GameObject($"Door_{i + 1}");
        Undo.RegisterCreatedObjectUndo(doorRoot, "Door root");
        doorRoot.transform.SetParent(parent.transform, false);
        doorRoot.transform.position = positions[i];

        var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(leaf, "Door leaf");
        leaf.name = "Leaf";
        leaf.transform.SetParent(doorRoot.transform, false);
        leaf.transform.localPosition = Vector3.zero;
        leaf.transform.localScale = new Vector3(0.3f, 2f, 1.5f);
        var lrb = leaf.GetComponent<Rigidbody>();
        if (lrb != null) Undo.DestroyObjectImmediate(lrb);

        var col = leaf.GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = false;

        Undo.AddComponent<DoorQuestionSlot>(doorRoot);
        var door = Undo.AddComponent<DoorInteractable>(doorRoot);

        var dso = new SerializedObject(door);
        dso.FindProperty("questionSlot").objectReferenceValue = doorRoot.GetComponent<DoorQuestionSlot>();
        dso.FindProperty("blockingCollider").objectReferenceValue = col;
        dso.FindProperty("doorLeaf").objectReferenceValue = leaf.transform;
        dso.ApplyModifiedPropertiesWithoutUndo();

        doors[i] = door;
      }

      return doors;
    }

    private static GameplayQuestionUI BuildQuestionUi(Transform root)
    {
      var canvasGo = new GameObject("GameplayCanvas");
      Undo.RegisterCreatedObjectUndo(canvasGo, "Canvas");
      canvasGo.transform.SetParent(root, false);

      var canvas = Undo.AddComponent<Canvas>(canvasGo);
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
      Undo.AddComponent<CanvasScaler>(canvasGo).uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
      canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
      Undo.AddComponent<GraphicRaycaster>(canvasGo);

      var panel = new GameObject("QuestionPanel");
      Undo.RegisterCreatedObjectUndo(panel, "Panel");
      panel.transform.SetParent(canvasGo.transform, false);
      var panelRect = panel.AddComponent<RectTransform>();
      panelRect.anchorMin = Vector2.zero;
      panelRect.anchorMax = Vector2.one;
      panelRect.offsetMin = Vector2.zero;
      panelRect.offsetMax = Vector2.zero;
      var img = Undo.AddComponent<Image>(panel);
      img.color = new Color(0f, 0f, 0f, 0.75f);

      var qTextGo = new GameObject("QuestionText");
      Undo.RegisterCreatedObjectUndo(qTextGo, "Q text");
      qTextGo.transform.SetParent(panel.transform, false);
      var qRect = qTextGo.AddComponent<RectTransform>();
      qRect.anchorMin = new Vector2(0.1f, 0.55f);
      qRect.anchorMax = new Vector2(0.9f, 0.9f);
      qRect.offsetMin = Vector2.zero;
      qRect.offsetMax = Vector2.zero;
      var tmp = Undo.AddComponent<TextMeshProUGUI>(qTextGo);
      tmp.text = "Soru burada";
      tmp.fontSize = 36;
      tmp.alignment = TextAlignmentOptions.Center;

      var buttons = new Button[4];
      for (int i = 0; i < 4; i++)
      {
        var btnGo = new GameObject($"Answer_{i + 1}");
        Undo.RegisterCreatedObjectUndo(btnGo, "Answer btn");
        btnGo.transform.SetParent(panel.transform, false);
        var btnRect = btnGo.AddComponent<RectTransform>();
        float yMin = 0.45f - i * 0.11f;
        btnRect.anchorMin = new Vector2(0.15f, yMin - 0.09f);
        btnRect.anchorMax = new Vector2(0.85f, yMin);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        var btnImg = Undo.AddComponent<Image>(btnGo);
        btnImg.color = new Color(0.2f, 0.25f, 0.35f, 1f);
        buttons[i] = Undo.AddComponent<Button>(btnGo);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = $"Cevap {i + 1}";
        label.fontSize = 28;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
      }

      panel.SetActive(false);

      var ui = Undo.AddComponent<GameplayQuestionUI>(canvasGo);
      var uiSo = new SerializedObject(ui);
      uiSo.FindProperty("panelRoot").objectReferenceValue = panel;
      uiSo.FindProperty("questionText").objectReferenceValue = tmp;
      uiSo.FindProperty("answerButtons").arraySize = 4;
      for (int i = 0; i < 4; i++)
        uiSo.FindProperty("answerButtons").GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
      uiSo.ApplyModifiedPropertiesWithoutUndo();

      return ui;
    }

    private static void EnsureQuestionManager(Transform root, DoorInteractable[] doors)
    {
      const string qRootName = "_CoopPuzzle_Questions";
      var existing = GameObject.Find(qRootName);
      GameObject qRoot;
      QuestionManager manager;

      if (existing != null)
      {
        qRoot = existing;
        manager = qRoot.GetComponent<QuestionManager>();
        if (manager == null)
          manager = Undo.AddComponent<QuestionManager>(qRoot);
      }
      else
      {
        qRoot = new GameObject(qRootName);
        Undo.RegisterCreatedObjectUndo(qRoot, "Questions root");
        qRoot.transform.SetParent(root, false);
        manager = Undo.AddComponent<QuestionManager>(qRoot);
      }

      var db = AssetDatabase.LoadAssetAtPath<QuestionDatabase>("Assets/ScriptableObjects/Questions/QuestionDatabase.asset");

      var mso = new SerializedObject(manager);
      mso.FindProperty("database").objectReferenceValue = db;
      mso.FindProperty("assignOnStart").boolValue = true;

      var doorsProp = mso.FindProperty("doorSlots");
      doorsProp.ClearArray();
      for (int i = 0; i < doors.Length; i++)
      {
        var slot = doors[i].GetComponent<DoorQuestionSlot>();
        if (slot == null) continue;
        doorsProp.InsertArrayElementAtIndex(doorsProp.arraySize);
        doorsProp.GetArrayElementAtIndex(doorsProp.arraySize - 1).objectReferenceValue = slot;
      }
      mso.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureEventSystem()
    {
      if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
        return;

      var esGo = new GameObject("EventSystem");
      Undo.RegisterCreatedObjectUndo(esGo, "EventSystem");
      Undo.AddComponent<UnityEngine.EventSystems.EventSystem>(esGo);
#if ENABLE_INPUT_SYSTEM
      Undo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(esGo);
#else
      Undo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>(esGo);
#endif
    }

    private static void BakeNavMesh(GameObject root)
    {
      var old = root.GetComponent<NavMeshSurface>();
      if (old != null) Undo.DestroyObjectImmediate(old);

      var surface = Undo.AddComponent<NavMeshSurface>(root);
      surface.collectObjects = CollectObjects.Children;
      surface.BuildNavMesh();
    }

    private static void WireFlow(QuestionFlowController flow, GameplayQuestionUI ui, GameObject player, UnityEngine.Camera cam)
    {
      var so = new SerializedObject(flow);
      so.FindProperty("questionUI").objectReferenceValue = ui;
      so.FindProperty("travelerMovement").objectReferenceValue = player.GetComponent<TravelerMovementController>();

      var touch = player.GetComponent<TravelerTouchInput>();
      var tso = new SerializedObject(touch);
      tso.FindProperty("inputCamera").objectReferenceValue = cam;
      tso.FindProperty("movement").objectReferenceValue = player.GetComponent<TravelerMovementController>();
      tso.ApplyModifiedPropertiesWithoutUndo();

      so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RemoveIfExists(string name)
    {
      var old = GameObject.Find(name);
      if (old != null) Undo.DestroyObjectImmediate(old);
    }

    private static void EnsureFolder(string unityPath)
    {
      if (AssetDatabase.IsValidFolder(unityPath)) return;
      var parent = System.IO.Path.GetDirectoryName(unityPath)?.Replace('\\', '/');
      var nm = System.IO.Path.GetFileName(unityPath);
      if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(nm)) return;
      if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
      AssetDatabase.CreateFolder(parent, nm);
    }
  }
}
