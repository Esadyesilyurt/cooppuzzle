namespace CoopPuzzle.Lobby
{
    /// <summary>Host panelindeki 4 slot: 0-1 Kırmızı takım, 2-3 Mavi takım.</summary>
    public static class LobbySlotLayout
    {
        public const int SlotCount = 4;

        public static (string team, string role) GetTeamRoleForSlot(int slotIndex)
        {
            return slotIndex switch
            {
                0 => (LobbyConstants.Team1Value, LobbyConstants.RoleTraveler),
                1 => (LobbyConstants.Team1Value, LobbyConstants.RoleSage),
                2 => (LobbyConstants.Team2Value, LobbyConstants.RoleTraveler),
                3 => (LobbyConstants.Team2Value, LobbyConstants.RoleSage),
                _ => (LobbyConstants.Team1Value, LobbyConstants.RoleTraveler)
            };
        }

        public static int GetSlotIndex(string team, string role)
        {
            var t = Normalize(team);
            var r = Normalize(role);
            if (t == LobbyConstants.Team1Value && r == LobbyConstants.RoleTraveler) return 0;
            if (t == LobbyConstants.Team1Value && r == LobbyConstants.RoleSage) return 1;
            if (t == LobbyConstants.Team2Value && r == LobbyConstants.RoleTraveler) return 2;
            if (t == LobbyConstants.Team2Value && r == LobbyConstants.RoleSage) return 3;
            return -1;
        }

        public static int GetSlotIndexForPlayer(Unity.Services.Lobbies.Models.Player player)
        {
            if (player?.Data == null) return -1;

            player.Data.TryGetValue(LobbyConstants.TeamKey, out var teamObj);
            player.Data.TryGetValue(LobbyConstants.RoleKey, out var roleObj);

            if (player.Data.TryGetValue(LobbyConstants.SlotKey, out var slotObj)
                && int.TryParse(slotObj.Value, out var explicitSlot)
                && explicitSlot is >= 0 and < SlotCount)
                return explicitSlot;

            return GetSlotIndex(teamObj?.Value, roleObj?.Value);
        }

        public static int FindFirstOpenSlotIndex(Unity.Services.Lobbies.Models.Lobby lobby)
        {
            if (lobby?.Players == null)
                return 0;

            var taken = new bool[SlotCount];
            foreach (var p in lobby.Players)
            {
                var idx = GetSlotIndexForPlayer(p);
                if (idx >= 0 && idx < taken.Length)
                    taken[idx] = true;
            }

            for (int i = 0; i < taken.Length; i++)
            {
                if (!taken[i])
                    return i;
            }

            return -1;
        }

        public static bool IsSlotOccupiedByOther(
            Unity.Services.Lobbies.Models.Lobby lobby,
            int slotIndex,
            string localPlayerId)
        {
            if (lobby?.Players == null || slotIndex < 0 || slotIndex >= SlotCount)
                return false;

            foreach (var p in lobby.Players)
            {
                if (p == null || p.Id == localPlayerId)
                    continue;

                if (GetSlotIndexForPlayer(p) == slotIndex)
                    return true;
            }

            return false;
        }

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
