using CoopPuzzle.Gameplay.Core;
using CoopPuzzle.Gameplay.Map;
using CoopPuzzle.Gameplay.Player;
using CoopPuzzle.Gameplay.Questions;
using CoopPuzzle.Gameplay.Sage;
using CoopPuzzle.Gameplay.UI;
using CoopPuzzle.Questions;
using TopDownCameraFollow = CoopPuzzle.Gameplay.Camera.TopDownCameraFollow;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoopPuzzle.EditorTools
{
    public static class CoopPuzzlePhase4OneClickSetup
    {
        private const string TargetScenePath = "Assets/Scenes/SampleScene.unity";
        private const string GameplayRootName = "_CoopPuzzle_Gameplay";
        private const string SageRootName = "_CoopPuzzle_Sage";

        [MenuItem("Tools/CoopPuzzle/Setup/Phase 4 Setup (Bilge — One Click)")]
        public static void SetupPhase4()
        {
            if (CoopPuzzleEditorPlayModeGuard.BlockIfPlaying("Phase 4 Setup"))
                return;

            if (!System.IO.File.Exists(TargetScenePath))
            {
                EditorUtility.DisplayDialog("CoopPuzzle", "Önce SampleScene ve Phase 3 kurulumu gerekli.", "OK");
                return;
            }

            EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

            var gameplayRoot = GameObject.Find(GameplayRootName);
            if (gameplayRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "CoopPuzzle",
                    "Phase 3 bulunamadı.\nÖnce: Tools → CoopPuzzle → Setup → Phase 3 Setup (One Click)",
                    "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Phase 4 Sage Setup");
            var group = Undo.GetCurrentGroup();

            EnsureSessionAndRouter(gameplayRoot);
            RemoveMissingScripts(gameplayRoot);
            EnsureTravelerTeamMarker(gameplayRoot);

            RemoveIfExists(SageRootName);
            var sageRoot = new GameObject(SageRootName);
            Undo.RegisterCreatedObjectUndo(sageRoot, "Sage root");
            sageRoot.transform.SetParent(gameplayRoot.transform, false);

            var sageUi = BuildSageDocumentUi(sageRoot.transform);
            var sageCam = BuildSageCamera(gameplayRoot.transform, sageRoot.transform);
            var flow = Undo.AddComponent<SageDocumentFlowController>(sageRoot);
            var bootstrap = Undo.AddComponent<SageSpectatorBootstrap>(sageRoot);

            WireSage(flow, bootstrap, sageUi, sageCam, gameplayRoot);
            WireCameraRouter(gameplayRoot, sageCam, sageUi);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(group);

            EditorUtility.DisplayDialog(
                "CoopPuzzle — Phase 4",
                "Bilge sistemi kuruldu.\n\n" +
                "• Gezgin: soru paneli | Bilge: belge paneli\n" +
                "• Tab ile rol değiştirme YOK (online iki oyuncu)\n" +
                "• Test: Tools → Test → Play As Traveler / Sage\n" +
                "  (Play'den ÖNCE rol seç)\n\n" +
                "Tek belge: Assets/ScriptableObjects/Sage/SageMasterDocument\n" +
                "(veya .txt → External Text File)\n" +
                "Sahneyi kaydet.",
                "OK");
        }

        private static void RemoveMissingScripts(GameObject gameplayRoot)
        {
            var removed = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameplayRoot);
            if (removed > 0)
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameplayRoot);
        }

        private static void EnsureSessionAndRouter(GameObject gameplayRoot)
        {
            if (gameplayRoot.GetComponent<GameplaySessionConfig>() == null)
                Undo.AddComponent<GameplaySessionConfig>(gameplayRoot);

            if (gameplayRoot.GetComponent<GameplayCameraRouter>() == null)
                Undo.AddComponent<GameplayCameraRouter>(gameplayRoot);
        }

        private static void EnsureTravelerTeamMarker(GameObject gameplayRoot)
        {
            var traveler = gameplayRoot.GetComponentInChildren<TravelerMovementController>(true);
            if (traveler == null) return;

            var marker = traveler.GetComponent<TravelerTeamMarker>();
            if (marker == null)
                marker = Undo.AddComponent<TravelerTeamMarker>(traveler.gameObject);

            var so = new SerializedObject(marker);
            so.FindProperty("team").enumValueIndex = (int)SpawnTeam.Team1;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SageDocumentUI BuildSageDocumentUi(Transform parent)
        {
            var canvasGo = new GameObject("SageDocumentCanvas");
            Undo.RegisterCreatedObjectUndo(canvasGo, "Sage canvas");
            canvasGo.transform.SetParent(parent, false);

            var canvas = Undo.AddComponent<Canvas>(canvasGo);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            var scaler = Undo.AddComponent<CanvasScaler>(canvasGo);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            Undo.AddComponent<GraphicRaycaster>(canvasGo);

            var panel = new GameObject("DocumentPanel");
            Undo.RegisterCreatedObjectUndo(panel, "Doc panel");
            panel.transform.SetParent(canvasGo.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(48f, 48f);
            panelRect.offsetMax = new Vector2(-48f, -48f);

            var panelImg = Undo.AddComponent<Image>(panel);
            panelImg.color = new Color(0.12f, 0.1f, 0.08f, 0.94f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.88f);
            titleRect.anchorMax = new Vector2(0.95f, 0.98f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleTmp = Undo.AddComponent<TextMeshProUGUI>(titleGo);
            titleTmp.text = "Bilgi Belgesi";
            titleTmp.fontSize = 40;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.95f, 0.88f, 0.65f);

            var hintGo = new GameObject("Hint");
            hintGo.transform.SetParent(panel.transform, false);
            var hintRect = hintGo.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.05f, 0.82f);
            hintRect.anchorMax = new Vector2(0.95f, 0.87f);
            hintRect.offsetMin = Vector2.zero;
            hintRect.offsetMax = Vector2.zero;
            var hintTmp = Undo.AddComponent<TextMeshProUGUI>(hintGo);
            hintTmp.text = "Gezgin bir kapıda — belgede ilgili bilgiyi bul.";
            hintTmp.fontSize = 24;
            hintTmp.fontStyle = FontStyles.Italic;
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.color = new Color(0.85f, 0.75f, 0.45f);
            hintGo.SetActive(false);

            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.05f, 0.12f);
            scrollRect.anchorMax = new Vector2(0.95f, 0.8f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;
            var scroll = Undo.AddComponent<ScrollRect>(scrollGo);
            scroll.horizontal = false;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            Undo.AddComponent<Mask>(viewport).showMaskGraphic = false;
            var viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = new Color(1f, 1f, 1f, 0.02f);

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 800f);

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(content.transform, false);
            var bodyRect = bodyGo.AddComponent<RectTransform>();
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(16f, 16f);
            bodyRect.offsetMax = new Vector2(-16f, -16f);
            var bodyTmp = Undo.AddComponent<TextMeshProUGUI>(bodyGo);
            bodyTmp.text = "Belge metni burada görünür.";
            bodyTmp.fontSize = 30;
            bodyTmp.alignment = TextAlignmentOptions.TopLeft;
            bodyTmp.color = new Color(0.92f, 0.9f, 0.85f);
            bodyTmp.enableWordWrapping = true;
            bodyTmp.overflowMode = TextOverflowModes.Overflow;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var scrollLayout = Undo.AddComponent<SageDocumentScrollLayout>(scrollGo);
            var slSo = new SerializedObject(scrollLayout);
            slSo.FindProperty("bodyText").objectReferenceValue = bodyTmp;
            slSo.ApplyModifiedPropertiesWithoutUndo();

            var closeGo = new GameObject("CloseButton");
            closeGo.transform.SetParent(panel.transform, false);
            var closeRect = closeGo.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.35f, 0.02f);
            closeRect.anchorMax = new Vector2(0.65f, 0.09f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
            var closeImg = Undo.AddComponent<Image>(closeGo);
            closeImg.color = new Color(0.35f, 0.28f, 0.2f, 1f);
            var closeBtn = Undo.AddComponent<Button>(closeGo);
            var closeLabelGo = new GameObject("Label");
            closeLabelGo.transform.SetParent(closeGo.transform, false);
            var closeLabelRect = closeLabelGo.AddComponent<RectTransform>();
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;
            var closeLabel = closeLabelGo.AddComponent<TextMeshProUGUI>();
            closeLabel.text = "Belgeyi Kapat";
            closeLabel.fontSize = 28;
            closeLabel.alignment = TextAlignmentOptions.Center;
            closeLabel.color = Color.white;

            panel.SetActive(false);

            var ui = Undo.AddComponent<SageDocumentUI>(canvasGo);
            var uiSo = new SerializedObject(ui);
            uiSo.FindProperty("panelRoot").objectReferenceValue = panel;
            uiSo.FindProperty("titleText").objectReferenceValue = titleTmp;
            uiSo.FindProperty("hintText").objectReferenceValue = hintTmp;
            uiSo.FindProperty("bodyText").objectReferenceValue = bodyTmp;
            uiSo.FindProperty("closeButton").objectReferenceValue = closeBtn;
            uiSo.FindProperty("scrollRect").objectReferenceValue = scroll;
            uiSo.FindProperty("scrollLayout").objectReferenceValue = scrollLayout;
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            return ui;
        }

        private static UnityEngine.Camera BuildSageCamera(Transform gameplayRoot, Transform sageRoot)
        {
            var traveler = gameplayRoot.GetComponentInChildren<TravelerMovementController>(true);
            var camGo = new GameObject("SageCamera");
            Undo.RegisterCreatedObjectUndo(camGo, "Sage camera");
            camGo.transform.SetParent(sageRoot, false);

            if (traveler != null)
                camGo.transform.position = traveler.transform.position + new Vector3(0f, 10f, -6f);
            camGo.transform.rotation = Quaternion.Euler(58f, 0f, 0f);

            var cam = Undo.AddComponent<UnityEngine.Camera>(camGo);
            cam.orthographic = true;
            cam.orthographicSize = 7.5f;
            cam.enabled = false;
            cam.depth = 1;

            var follow = Undo.AddComponent<TopDownCameraFollow>(camGo);
            var so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = traveler != null ? traveler.transform : null;
            so.FindProperty("viewCamera").objectReferenceValue = cam;
            so.FindProperty("offset").vector3Value = new Vector3(0f, 10f, -5.5f);
            so.FindProperty("orthographicSize").floatValue = 7.5f;
            so.ApplyModifiedPropertiesWithoutUndo();

            return cam;
        }

        private static void WireSage(
            SageDocumentFlowController flow,
            SageSpectatorBootstrap bootstrap,
            SageDocumentUI ui,
            UnityEngine.Camera sageCam,
            GameObject gameplayRoot)
        {
            var session = gameplayRoot.GetComponent<GameplaySessionConfig>();
            var masterDoc = EnsureMasterDocumentAsset();
            var fso = new SerializedObject(flow);
            fso.FindProperty("session").objectReferenceValue = session;
            fso.FindProperty("documentUI").objectReferenceValue = ui;
            fso.FindProperty("masterDocument").objectReferenceValue = masterDoc;
            fso.FindProperty("watchTeam").enumValueIndex = (int)SpawnTeam.Team1;
            fso.ApplyModifiedPropertiesWithoutUndo();

            var bso = new SerializedObject(bootstrap);
            bso.FindProperty("team").enumValueIndex = (int)SpawnTeam.Team1;
            bso.FindProperty("followCamera").objectReferenceValue = sageCam.GetComponent<TopDownCameraFollow>();
            bso.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireCameraRouter(GameObject gameplayRoot, UnityEngine.Camera sageCam, SageDocumentUI sageUi)
        {
            var router = gameplayRoot.GetComponent<GameplayCameraRouter>();
            if (router == null) return;

            var travelerCam = gameplayRoot.GetComponentInChildren<UnityEngine.Camera>(true);
            foreach (var c in gameplayRoot.GetComponentsInChildren<UnityEngine.Camera>(true))
            {
                if (c != null && c.gameObject.name == "GameplayCamera")
                {
                    travelerCam = c;
                    break;
                }
            }

            var traveler = gameplayRoot.GetComponentInChildren<TravelerMovementController>(true);
            var touch = traveler != null ? traveler.GetComponent<TravelerTouchInput>() : null;
            var questionUi = gameplayRoot.GetComponentInChildren<GameplayQuestionUI>(true);
            var sageFlow = gameplayRoot.GetComponentInChildren<SageDocumentFlowController>(true);

            var rso = new SerializedObject(router);
            rso.FindProperty("session").objectReferenceValue = gameplayRoot.GetComponent<GameplaySessionConfig>();
            rso.FindProperty("travelerCamera").objectReferenceValue = travelerCam;
            rso.FindProperty("sageCamera").objectReferenceValue = sageCam;
            rso.FindProperty("travelerInput").objectReferenceValue = touch;
            rso.FindProperty("travelerQuestionUI").objectReferenceValue = questionUi;
            rso.FindProperty("sageDocumentUI").objectReferenceValue = sageUi;
            rso.FindProperty("sageDocumentFlow").objectReferenceValue = sageFlow;
            rso.ApplyModifiedPropertiesWithoutUndo();

            router.ApplyRole();
        }

        private static void RemoveIfExists(string name)
        {
            var old = GameObject.Find(name);
            if (old != null)
                Undo.DestroyObjectImmediate(old);
        }

        private static SageMasterDocument EnsureMasterDocumentAsset()
        {
            const string folder = "Assets/ScriptableObjects/Sage";
            const string path = folder + "/SageMasterDocument.asset";
            EnsureFolder(folder);

            var existing = AssetDatabase.LoadAssetAtPath<SageMasterDocument>(path);
            if (existing != null)
                return existing;

            var doc = ScriptableObject.CreateInstance<SageMasterDocument>();
            var so = new SerializedObject(doc);
            so.FindProperty("title").stringValue = "Bilge El Kitabı";
            so.FindProperty("bodyText").stringValue =
                "Bu tek ana belgedir. Tüm kapı sorularının ipuçlarını buraya yaz.\n\n" +
                "Örnek bölüm başlıkları:\n" +
                "=== Tarih ===\n" +
                "• I. Dünya Savaşı antlaşmaları...\n\n" +
                "=== Coğrafya ===\n" +
                "• ...\n\n" +
                "Gezgin kapıda soru çözerken Bilge bu metinde arama yapar.";
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(doc, path);
            AssetDatabase.SaveAssets();
            return doc;
        }

        private static void EnsureFolder(string unityPath)
        {
            if (AssetDatabase.IsValidFolder(unityPath)) return;
            var parent = System.IO.Path.GetDirectoryName(unityPath)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(unityPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
