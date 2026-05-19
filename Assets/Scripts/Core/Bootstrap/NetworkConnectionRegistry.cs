using System.Collections.Generic;
using System.Text;

namespace CoopPuzzle.Core.Bootstrap
{
    /// <summary>Host: NGO clientId → UGS Authentication playerId.</summary>
    public static class NetworkConnectionRegistry
    {
        private static readonly Dictionary<ulong, string> ClientToPlayerId = new();
        private static readonly Dictionary<string, ulong> PlayerIdToClient = new();

        public static void Clear()
        {
            ClientToPlayerId.Clear();
            PlayerIdToClient.Clear();
        }

        public static void Register(ulong clientNetworkId, string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return;

            ClientToPlayerId[clientNetworkId] = playerId;
            PlayerIdToClient[playerId] = clientNetworkId;
        }

        public static bool TryGetPlayerId(ulong clientNetworkId, out string playerId) =>
            ClientToPlayerId.TryGetValue(clientNetworkId, out playerId);

        public static bool TryGetClientId(string playerId, out ulong clientNetworkId) =>
            PlayerIdToClient.TryGetValue(playerId, out clientNetworkId);

        public static byte[] EncodePlayerId(string playerId) =>
            string.IsNullOrEmpty(playerId) ? System.Array.Empty<byte>() : Encoding.UTF8.GetBytes(playerId);

        public static string DecodePlayerId(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                return string.Empty;

            return Encoding.UTF8.GetString(payload);
        }
    }
}
