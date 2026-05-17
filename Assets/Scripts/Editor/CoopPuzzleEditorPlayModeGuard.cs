using UnityEditor;
using UnityEngine;

namespace CoopPuzzle.EditorTools
{
    internal static class CoopPuzzleEditorPlayModeGuard
    {
        /// <returns>true if caller should abort (Play mode active).</returns>
        public static bool BlockIfPlaying(string operationName)
        {
            if (!EditorApplication.isPlaying)
                return false;

            EditorUtility.DisplayDialog(
                "CoopPuzzle",
                $"{operationName} Play modunda çalışmaz.\n\nÖnce Play'i durdur, sonra menüyü tekrar çalıştır.",
                "OK");
            return true;
        }
    }
}
