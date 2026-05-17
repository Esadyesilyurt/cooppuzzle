#if UNITY_EDITOR
using CoopPuzzle.Gameplay.Core;
using CoopPuzzle.Gameplay.Map;
using UnityEditor;
using UnityEngine;

namespace CoopPuzzle.EditorTools
{
    /// <summary>
    /// ParrelSync klonunda Bilge, ana editörde Gezgin (Play öncesi otomatik rol).
    /// </summary>
    [InitializeOnLoad]
    public static class ParrelSyncGameplayRoleBootstrap
    {
        static ParrelSyncGameplayRoleBootstrap()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            if (!TryIsParrelSyncClone(out var isClone))
                return;

            var config = Object.FindAnyObjectByType<GameplaySessionConfig>(FindObjectsInactive.Include);
            if (config == null)
                return;

            if (isClone)
            {
                config.SetLocalRole(GameplayRole.Sage);
                config.SetLocalTeam(SpawnTeam.Team1);
                Debug.Log("[ParrelSync] Klon → Bilge rolü atandı.");
            }
            else
            {
                config.SetLocalRole(GameplayRole.Traveler);
                config.SetLocalTeam(SpawnTeam.Team1);
                Debug.Log("[ParrelSync] Ana proje → Gezgin rolü atandı.");
            }
        }

        private static bool TryIsParrelSyncClone(out bool isClone)
        {
            isClone = false;
            var type = System.Type.GetType("ParrelSync.ClonesManager, ParrelSync");
            if (type == null)
                return false;

            var method = type.GetMethod(
                "IsClone",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method == null)
                return false;

            isClone = (bool)method.Invoke(null, null);
            return true;
        }
    }
}
#endif
