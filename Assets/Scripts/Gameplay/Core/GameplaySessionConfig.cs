using CoopPuzzle.Gameplay.Map;
using UnityEngine;

namespace CoopPuzzle.Gameplay.Core
{
    /// <summary>
    /// Yerel oyuncu rolü ve takımı (test / ileride NGO ile senkron).
    /// </summary>
    public sealed class GameplaySessionConfig : MonoBehaviour
    {
        public static GameplaySessionConfig Instance { get; private set; }

        [SerializeField] private SpawnTeam localTeam = SpawnTeam.Team1;
        [SerializeField] private GameplayRole localRole = GameplayRole.Traveler;

        public SpawnTeam LocalTeam => localTeam;
        public GameplayRole LocalRole => localRole;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetLocalRole(GameplayRole role) => localRole = role;

        public void SetLocalTeam(SpawnTeam team) => localTeam = team;

        public void ToggleRole() =>
            localRole = localRole == GameplayRole.Traveler ? GameplayRole.Sage : GameplayRole.Traveler;
    }
}
