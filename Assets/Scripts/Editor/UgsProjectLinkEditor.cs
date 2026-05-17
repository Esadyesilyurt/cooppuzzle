using UnityEditor;
using UnityEngine;

namespace CoopPuzzle.EditorTools
{
    public static class UgsProjectLinkEditor
    {
        public static bool IsLinked =>
            !string.IsNullOrEmpty(PlayerSettings.cloudProjectId);

        public static string GetLinkInstructions() =>
            "Unity projesi Cloud'a bağlı değil.\n" +
            "1) Edit > Project Settings > Services\n" +
            "2) Create / Link Unity Project ID\n" +
            "3) Dashboard'da Lobby ve Relay servislerini etkinleştir\n" +
            "4) Sahneyi kaydet ve tekrar dene.";
    }
}
