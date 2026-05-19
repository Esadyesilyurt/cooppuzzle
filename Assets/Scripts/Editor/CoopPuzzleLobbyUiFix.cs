using System;
using CoopPuzzle.Lobby;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoopPuzzle.EditorTools
{
    public static class CoopPuzzleLobbyUiFix
    {
        [MenuItem("Tools/CoopPuzzle/Setup/Fix Lobby UI (Visibility + Wiring)")]
        public static void FixLobbyUi()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded)
            {
                EditorUtility.DisplayDialog("CoopPuzzle", "Önce menü sahnesini aç.", "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Fix Lobby UI");

            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("CoopPuzzle", "Canvas bulunamadı.", "OK");
                return;
            }

            var lobbyUi = canvas.GetComponent<LobbyUIManager>();
            var gameLobby = canvas.GetComponent<GameLobbyController>();

            MakeTextReadable(FindTmpByName("KOD: XXXX"), 42f);
            var lobipanel = FindGo("lobipanel");
            HideMainMenuOdaKodu(lobipanel);
            var odaKodu = FindJoinPanelInput("OdaKodu");
            if (odaKodu != null)
            {
                if (odaKodu.textComponent != null)
                    MakeTextReadable(odaKodu.textComponent, 36f);
                if (odaKodu.placeholder is TextMeshProUGUI ph)
                {
                    ph.text = "Oda kodunu girin (6 karakter)";
                    MakeTextReadable(ph, 28f, new Color(1f, 1f, 1f, 0.55f));
                }
                odaKodu.characterLimit = CoopPuzzle.Lobby.LobbyConstants.LobbyCodeLength;
                odaKodu.contentType = TMP_InputField.ContentType.Alphanumeric;
            }

            var durum = FindTmpByName("Durum") ?? CreateStatusText(canvas.transform);
            MakeTextReadable(durum, 28f);

            WireMainMenuJoinButton(gameLobby);
            WireJoinPanelButtons(gameLobby);
            WirePanelBackButtons(gameLobby);
            EnsureHostPanelController(hostPanel: FindGo("HostPanel"), gameLobby);

            if (gameLobby != null)
            {
                var so = new SerializedObject(gameLobby);
                so.FindProperty("lobbyUi").objectReferenceValue = lobbyUi;
                so.FindProperty("hostRoomCodeText").objectReferenceValue = FindTmpByName("KOD: XXXX");
                so.FindProperty("joinRoomCodeInput").objectReferenceValue = odaKodu;
                so.FindProperty("joinPlayerNameInput").objectReferenceValue = FindJoinPanelInput("Isim");
                so.FindProperty("statusText").objectReferenceValue = durum;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            EditorUtility.DisplayDialog(
                "CoopPuzzle",
                "Lobby UI düzeltildi.\n\n- lobi bağlan → JoinPanel\n- Bağlan → HostPanel (client)\n- KOD metni beyaz / büyük\n\nSahneyi kaydet (Ctrl+S).",
                "OK");
        }

        private static GameObject FindGo(string name)
        {
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t != null && t.gameObject.name == name)
                    return t.gameObject;
            }
            return null;
        }

        private static TextMeshProUGUI FindTmpByName(string name)
        {
            foreach (var t in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
            {
                if (t != null && t.gameObject.name == name)
                    return t;
            }
            return null;
        }

        private static TMP_InputField FindInputByName(string name)
        {
            foreach (var i in UnityEngine.Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include))
            {
                if (i != null && i.gameObject.name == name)
                    return i;
            }
            return null;
        }

        private static TextMeshProUGUI CreateStatusText(Transform parent)
        {
            var go = new GameObject("Durum");
            Undo.RegisterCreatedObjectUndo(go, "Create Durum");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(900f, 80f);
            rect.anchoredPosition = new Vector2(0f, 40f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "Lobby durumu";
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static void HideMainMenuOdaKodu(GameObject lobipanel)
        {
            if (lobipanel == null) return;
            foreach (var i in lobipanel.GetComponentsInChildren<TMP_InputField>(true))
            {
                if (i != null && i.gameObject.name.IndexOf("OdaKodu", StringComparison.OrdinalIgnoreCase) >= 0)
                    i.gameObject.SetActive(false);
            }
        }

        private static TMP_InputField FindJoinPanelInput(string fieldName)
        {
            var joinPanel = FindGo("JoinPanel");
            if (joinPanel == null) return null;
            foreach (var i in joinPanel.GetComponentsInChildren<TMP_InputField>(true))
            {
                if (i != null && string.Equals(i.gameObject.name, fieldName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return null;
        }

        private static void WirePanelBackButtons(GameLobbyController gameLobby)
        {
            if (gameLobby == null) return;
            WireBackInPanel(FindGo("HostPanel"), gameLobby);
            WireBackInPanel(FindGo("JoinPanel"), gameLobby);
        }

        private static void WireBackInPanel(GameObject panel, GameLobbyController gameLobby)
        {
            if (panel == null) return;
            foreach (var b in panel.GetComponentsInChildren<Button>(true))
            {
                if (b == null) continue;
                var n = b.gameObject.name ?? string.Empty;
                if (!n.Contains("Geri", System.StringComparison.OrdinalIgnoreCase)
                    && !n.Equals("Back", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                Undo.RecordObject(b, "Wire panel Geri");
                var onClick = b.onClick;
                for (int i = onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(onClick, i);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(onClick, gameLobby.ReturnToLobbyPanel);
                EditorUtility.SetDirty(b);
            }
        }

        private static void WireJoinPanelButtons(GameLobbyController gameLobby)
        {
            if (gameLobby == null) return;
            var joinPanel = FindGo("JoinPanel");
            if (joinPanel == null) return;

            foreach (var b in joinPanel.GetComponentsInChildren<Button>(true))
            {
                if (b == null) continue;
                var n = b.gameObject.name ?? string.Empty;

                if (n.Equals("Bağlan", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("Baglan", StringComparison.OrdinalIgnoreCase))
                {
                    Undo.RecordObject(b, "Wire JoinPanel Bağlan");
                    var onClick = b.onClick;
                    for (int i = onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(onClick, i);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(onClick, gameLobby.JoinLobby);
                    EditorUtility.SetDirty(b);
                }
            }
        }

        private static void EnsureHostPanelController(GameObject hostPanel, GameLobbyController gameLobby)
        {
            if (hostPanel == null) return;

            var hostUi = hostPanel.GetComponent<HostPanelController>();
            if (hostUi == null)
                hostUi = Undo.AddComponent<HostPanelController>(hostPanel);

            if (gameLobby == null) return;
            var so = new SerializedObject(hostUi);
            so.FindProperty("gameLobby").objectReferenceValue = gameLobby;
            so.FindProperty("lobbyCoordinator").objectReferenceValue =
                UnityEngine.Object.FindAnyObjectByType<LobbyCoordinator>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireMainMenuJoinButton(GameLobbyController gameLobby)
        {
            if (gameLobby == null) return;
            foreach (var b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
            {
                if (b == null) continue;
                var n = b.gameObject.name ?? string.Empty;
                if (!n.Contains("lobi", StringComparison.OrdinalIgnoreCase)) continue;
                if (!n.Contains("bağlan", StringComparison.OrdinalIgnoreCase) &&
                    !n.Contains("baglan", StringComparison.OrdinalIgnoreCase))
                    continue;

                Undo.RecordObject(b, "Wire lobi bağlan");
                var onClick = b.onClick;
                for (int i = onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(onClick, i);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(onClick, gameLobby.OpenJoinMenu);
                EditorUtility.SetDirty(b);
                return;
            }
        }

        private static void MakeTextReadable(TMP_Text tmp, float fontSize, Color? color = null)
        {
            if (tmp == null) return;
            Undo.RecordObject(tmp, "Make TMP readable");
            tmp.color = color ?? Color.white;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.ForceMeshUpdate();

            // Outline requires initialized font material; skip if not ready (avoids editor NRE).
            if (tmp.fontSharedMaterial != null)
            {
                try
                {
                    tmp.outlineWidth = 0.2f;
                    tmp.outlineColor = Color.black;
                }
                catch
                {
                    // Outline optional; color + size are enough for visibility.
                }
            }
        }
    }
}
