using CoopPuzzle.Gameplay.Core;
using CoopPuzzle.Gameplay.Map;
using Unity.Services.Authentication;
using UnityEngine;

namespace CoopPuzzle.Lobby
{
    public static class LobbyRoleAssignment
    {
        public static bool TryGetRoleForLobbyPlayer(Unity.Services.Lobbies.Models.Player player, out GameplayRole role)
        {
            role = GameplayRole.Traveler;
            if (player == null)
                return false;

            var slot = LobbySlotLayout.GetSlotIndexForPlayer(player);
            if (slot < 0)
                slot = 0;

            if (player.Data != null
                && player.Data.TryGetValue(LobbyConstants.RoleKey, out var roleObj)
                && TryParseRole(roleObj.Value, out var parsedRole))
            {
                role = parsedRole;
                return true;
            }

            role = GetDefaultRoleForSlot(slot);
            return true;
        }

        public static bool TryGetTeamForLobbyPlayer(Unity.Services.Lobbies.Models.Player player, out SpawnTeam team)
        {
            team = SpawnTeam.Team1;
            if (player == null)
                return false;

            var slot = LobbySlotLayout.GetSlotIndexForPlayer(player);
            if (slot < 0)
                slot = 0;

            if (player.Data != null
                && player.Data.TryGetValue(LobbyConstants.TeamKey, out var teamObj)
                && TryParseTeam(teamObj.Value, out var parsedTeam))
            {
                team = parsedTeam;
                return true;
            }

            team = GetDefaultTeamForSlot(slot);
            return true;
        }

        public static void ApplyLocalRoleFromLobby(GameplaySessionConfig config)
        {
            if (config == null) return;

            var coordinator = Object.FindAnyObjectByType<LobbyCoordinator>();
            var lobby = coordinator?.LobbyService?.CurrentLobby;

            if (lobby?.Players == null || lobby.Players.Count == 0)
            {
                Debug.LogWarning("[LobbyRole] Lobby verisi yok; varsayılan Gezgin Team1.");
                config.SetLocalTeam(SpawnTeam.Team1);
                config.SetLocalRole(GameplayRole.Traveler);
                return;
            }

            var localId = AuthenticationService.Instance.PlayerId;
            var localPlayer = lobby.Players.Find(p => p.Id == localId);
            var slot = localPlayer != null ? LobbySlotLayout.GetSlotIndexForPlayer(localPlayer) : 0;
            if (slot < 0)
                slot = 0;

            if (localPlayer?.Data != null
                && localPlayer.Data.TryGetValue(LobbyConstants.RoleKey, out var roleObj)
                && TryParseRole(roleObj.Value, out var parsedRole))
            {
                config.SetLocalRole(parsedRole);
            }
            else
            {
                config.SetLocalRole(GetDefaultRoleForSlot(slot));
            }

            if (localPlayer?.Data != null
                && localPlayer.Data.TryGetValue(LobbyConstants.TeamKey, out var teamObj)
                && TryParseTeam(teamObj.Value, out var parsedTeam))
            {
                config.SetLocalTeam(parsedTeam);
            }
            else
            {
                config.SetLocalTeam(GetDefaultTeamForSlot(slot));
            }

            Debug.Log($"[LobbyRole] Slot {slot} → {config.LocalTeam} / {config.LocalRole}");
        }

        private static GameplayRole GetDefaultRoleForSlot(int index) =>
            index switch
            {
                1 => GameplayRole.Sage,
                3 => GameplayRole.Sage,
                _ => GameplayRole.Traveler
            };

        private static SpawnTeam GetDefaultTeamForSlot(int index) =>
            index < 2 ? SpawnTeam.Team1 : SpawnTeam.Team2;

        private static bool TryParseRole(string value, out GameplayRole role)
        {
            role = GameplayRole.Traveler;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var v = value.Trim().ToLowerInvariant();
            if (v == LobbyConstants.RoleSage || v == "bilge" || v == "sage")
            {
                role = GameplayRole.Sage;
                return true;
            }

            if (v == LobbyConstants.RoleTraveler || v == "gezgin" || v == "traveler")
            {
                role = GameplayRole.Traveler;
                return true;
            }

            return false;
        }

        private static bool TryParseTeam(string value, out SpawnTeam team)
        {
            team = SpawnTeam.Team1;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var v = value.Trim().ToLowerInvariant();
            if (v == LobbyConstants.Team2Value || v == "2" || v == "team2")
            {
                team = SpawnTeam.Team2;
                return true;
            }

            if (v == LobbyConstants.Team1Value || v == "1" || v == "team1")
            {
                team = SpawnTeam.Team1;
                return true;
            }

            return false;
        }
    }
}
