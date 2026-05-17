#if UNITY_EDITOR
using CoopPuzzle.Gameplay.Sage;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CoopPuzzle.EditorTools
{
    public static class CoopPuzzleSageDocumentScrollFix
    {
        [MenuItem("Tools/CoopPuzzle/Sage/Fix Document Scroll (Existing UI)")]
        public static void FixExistingDocumentScroll()
        {
            if (CoopPuzzleEditorPlayModeGuard.BlockIfPlaying("Sage scroll düzeltmesi"))
                return;

            var scroll = Object.FindAnyObjectByType<ScrollRect>(FindObjectsInactive.Include);
            if (scroll == null || scroll.gameObject.name != "ScrollView")
            {
                foreach (var sr in Object.FindObjectsByType<ScrollRect>(FindObjectsInactive.Include))
                {
                    if (sr.GetComponentInParent<SageDocumentUI>(true) != null)
                    {
                        scroll = sr;
                        break;
                    }
                }
            }

            if (scroll == null)
            {
                EditorUtility.DisplayDialog("CoopPuzzle", "Sage ScrollView bulunamadı. Phase 4 Setup çalıştır.", "OK");
                return;
            }

            var body = scroll.content != null
                ? scroll.content.GetComponentInChildren<TextMeshProUGUI>(true)
                : null;

            if (body == null)
            {
                EditorUtility.DisplayDialog("CoopPuzzle", "Body TextMeshPro bulunamadı.", "OK");
                return;
            }

            var layout = scroll.GetComponent<SageDocumentScrollLayout>();
            if (layout == null)
                layout = Undo.AddComponent<SageDocumentScrollLayout>(scroll.gameObject);

            var so = new SerializedObject(layout);
            so.FindProperty("bodyText").objectReferenceValue = body;
            so.ApplyModifiedProperties();

            body.enableWordWrapping = true;
            body.overflowMode = TextOverflowModes.Overflow;

            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var ui = scroll.GetComponentInParent<SageDocumentUI>(true);
            if (ui != null)
            {
                var uiSo = new SerializedObject(ui);
                uiSo.FindProperty("scrollRect").objectReferenceValue = scroll;
                uiSo.FindProperty("scrollLayout").objectReferenceValue = layout;
                uiSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(scroll.gameObject);
            EditorUtility.DisplayDialog(
                "CoopPuzzle",
                "Belge kaydırma düzeltildi.\n\nPlay'de uzun metin aşağı kaydırılabilir.\nSahneyi kaydet.",
                "OK");
        }
    }
}
#endif
