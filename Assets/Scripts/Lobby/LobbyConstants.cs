using System;
using System.Text;

namespace CoopPuzzle.Lobby
{
    public static class LobbyConstants
    {
        public const int MaxPlayers = 4;

        /// <summary>BAŞLAT için minimum oyuncu (geliştirme/test: 2, yayın: 4).</summary>
        public const int MinPlayersToStart = 2;

        /// <summary>Unity Lobby servisinin ürettiği kod uzunluğu.</summary>
        public const int LobbyCodeLength = 6;

        public static string NormalizeLobbyCode(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var sb = new StringBuilder(raw.Length);
            foreach (var c in raw.Trim().ToUpperInvariant())
            {
                if (c is >= 'A' and <= 'Z' or >= '0' and <= '9')
                    sb.Append(c);
            }

            return sb.ToString();
        }

        public static bool TryValidateLobbyCode(string raw, out string code, out string error)
        {
            code = NormalizeLobbyCode(raw);
            if (string.IsNullOrEmpty(code))
            {
                error = $"Lobi kodu girin ({LobbyCodeLength} karakter, host ekranındaki kod).";
                return false;
            }

            if (code.Length != LobbyCodeLength)
            {
                error = $"Lobi kodu tam {LobbyCodeLength} karakter olmalı (girilen: {code.Length}).";
                return false;
            }

            error = null;
            return true;
        }

        // Lobby Data keys
        public const string RelayJoinCodeKey = "relayJoinCode";

        // Player Data keys
        public const string PlayerNameKey = "playerName";
        public const string TeamKey = "team";
        public const string RoleKey = "role";
        public const string ReadyKey = "ready";
        public const string SlotKey = "slot";

        public const string GameStateKey = "gameState";
        public const string GameStateStarted = "started";
        public const string GameStateFinished = "finished";
        public const string GameStateClosed = "closed";

        public static bool IsGameStarted(Unity.Services.Lobbies.Models.Lobby lobby)
        {
            if (lobby?.Data == null)
                return false;

            return lobby.Data.TryGetValue(GameStateKey, out var state)
                   && string.Equals(state.Value, GameStateStarted, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsGameFinished(Unity.Services.Lobbies.Models.Lobby lobby)
        {
            if (lobby?.Data == null)
                return false;

            return lobby.Data.TryGetValue(GameStateKey, out var state)
                   && string.Equals(state.Value, GameStateFinished, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsGameClosed(Unity.Services.Lobbies.Models.Lobby lobby)
        {
            if (lobby?.Data == null)
                return false;

            return lobby.Data.TryGetValue(GameStateKey, out var state)
                   && string.Equals(state.Value, GameStateClosed, StringComparison.OrdinalIgnoreCase);
        }

        public const string RoleTraveler = "traveler";
        public const string RoleSage = "sage";
        public const string Team1Value = "team1";
        public const string Team2Value = "team2";
    }
}

