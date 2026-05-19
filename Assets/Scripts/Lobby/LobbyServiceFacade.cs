using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace CoopPuzzle.Lobby
{
    using LobbyModel = Unity.Services.Lobbies.Models.Lobby;

    public sealed class LobbyServiceFacade
    {
        private const string PrefsLobbyIdPrefix = "CoopPuzzle.LastLobbyId.";
        private const string PrefsLobbyCodePrefix = "CoopPuzzle.LastLobbyCode.";

        public LobbyModel CurrentLobby { get; private set; }

        private CancellationTokenSource _pollCts;
        private CancellationTokenSource _heartbeatCts;
        private Action _onLobbyClosed;

        public async Task<LobbyModel> CreateLobbyAsync(string lobbyName, int maxPlayers, Dictionary<string, PlayerDataObject> playerData)
        {
            try
            {
                var options = new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Player = new Player { Data = playerData }
                };

                CurrentLobby = await Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
                RememberLobby(CurrentLobby);
                return CurrentLobby;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lobby Create hata: {ex}");
                throw;
            }
        }

        public async Task<LobbyModel> JoinLobbyByCodeAsync(string lobbyCode, Dictionary<string, PlayerDataObject> playerData)
        {
            var normalizedCode = LobbyConstants.NormalizeLobbyCode(lobbyCode);

            if (CurrentLobby != null &&
                string.Equals(CurrentLobby.LobbyCode, normalizedCode, StringComparison.OrdinalIgnoreCase))
            {
                var refreshed = await ReconnectOrGetLobbyAsync(CurrentLobby.Id, CurrentLobby);
                CurrentLobby = refreshed ?? CurrentLobby;
                return await CompleteJoinAsync(playerData);
            }

            var alreadyJoined = await TryResolveJoinedLobbyByCodeAsync(normalizedCode);
            if (alreadyJoined != null)
            {
                CurrentLobby = alreadyJoined;
                return await CompleteJoinAsync(playerData);
            }

            var joinOptions = new JoinLobbyByCodeOptions
            {
                Player = new Player { Data = playerData }
            };

            try
            {
                CurrentLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(normalizedCode, joinOptions);
                return await CompleteJoinAsync(playerData);
            }
            catch (LobbyServiceException ex) when (IsAlreadyMemberError(ex))
            {
                var recovered = await RecoverAlreadyJoinedLobbyAsync(normalizedCode, joinOptions);
                if (recovered != null)
                {
                    CurrentLobby = recovered;
                    return await CompleteJoinAsync(playerData);
                }

                Debug.LogWarning($"Lobby JoinByCode: zaten üye ama lobby kurtarılamadı ({normalizedCode}).");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lobby JoinByCode hata: {ex}");
                throw;
            }
        }

        public void Shutdown()
        {
            StopPolling();
            StopHeartbeat();
            _onLobbyClosed = null;
            _ = LeaveKnownLobbiesAsync(silent: true);
        }

        public async Task DeleteCurrentLobbyAsync()
        {
            if (CurrentLobby == null || string.IsNullOrEmpty(CurrentLobby.Id))
                return;

            StopPolling();
            StopHeartbeat();

            var lobbyId = CurrentLobby.Id;
            try
            {
                await Lobbies.Instance.DeleteLobbyAsync(lobbyId);
            }
            catch (LobbyServiceException ex) when (IsLobbyUnavailableError(ex))
            {
                // Zaten silinmiş olabilir.
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Lobby Delete uyarı ({lobbyId}): {ex.Message}");
            }

            CurrentLobby = null;
            ClearRememberedLobby();
        }

        public async Task LeaveCurrentLobbyAsync()
        {
            StopPolling();
            StopHeartbeat();
            await LeaveKnownLobbiesAsync(silent: true);
        }

        public bool HasActiveLobby() =>
            CurrentLobby != null && !string.IsNullOrEmpty(CurrentLobby.Id);

        private static bool IsAlreadyMemberError(LobbyServiceException ex)
        {
            if (ex.Reason is LobbyExceptionReason.Conflict or LobbyExceptionReason.LobbyConflict)
                return true;

            var msg = ex.Message ?? string.Empty;
            return msg.IndexOf("already a member", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public bool IsLocalPlayerInLobby()
        {
            if (CurrentLobby?.Players == null)
                return false;

            var localId = AuthenticationService.Instance.PlayerId;
            return CurrentLobby.Players.Exists(p => p != null && p.Id == localId);
        }

        public async Task EnsureLocalPlayerSlotAssignedAsync(string playerName)
        {
            if (CurrentLobby == null || !IsLocalPlayerInLobby())
                return;

            try
            {
                CurrentLobby = await Lobbies.Instance.GetLobbyAsync(CurrentLobby.Id);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Lobby refresh before slot assign: {ex.Message}");
            }

            var lobby = CurrentLobby;
            if (lobby?.Players == null)
                return;

            var localId = AuthenticationService.Instance.PlayerId;
            var localPlayer = GetLocalPlayer();
            var slot = localPlayer != null ? LobbySlotLayout.GetSlotIndexForPlayer(localPlayer) : -1;

            if (slot < 0 || LobbySlotLayout.IsSlotOccupiedByOther(lobby, slot, localId))
                slot = LobbySlotLayout.FindFirstOpenSlotIndex(lobby);

            if (slot < 0)
            {
                Debug.LogWarning("Lobby dolu; boş slot yok.");
                return;
            }

            playerName = string.IsNullOrWhiteSpace(playerName) ? "Oyuncu" : playerName.Trim();
            var (team, role) = LobbySlotLayout.GetTeamRoleForSlot(slot);
            var options = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { LobbyConstants.PlayerNameKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                    { LobbyConstants.TeamKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, team) },
                    { LobbyConstants.RoleKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, role) },
                    { LobbyConstants.SlotKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, slot.ToString()) },
                    { LobbyConstants.ReadyKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0") },
                }
            };

            CurrentLobby = await Lobbies.Instance.UpdatePlayerAsync(CurrentLobby.Id, localId, options);
        }

        private async Task<LobbyModel> CompleteJoinAsync(Dictionary<string, PlayerDataObject> playerData)
        {
            var playerName = ExtractPlayerName(playerData);
            await EnsureLocalPlayerSlotAssignedAsync(playerName);
            RememberLobby(CurrentLobby);
            return CurrentLobby;
        }

        private static string ExtractPlayerName(Dictionary<string, PlayerDataObject> playerData)
        {
            if (playerData != null &&
                playerData.TryGetValue(LobbyConstants.PlayerNameKey, out var nameObj) &&
                !string.IsNullOrWhiteSpace(nameObj.Value))
                return nameObj.Value.Trim();

            return "Oyuncu";
        }

        private static string PrefsLobbyIdKey =>
            PrefsLobbyIdPrefix + AuthenticationService.Instance.PlayerId;

        private static string PrefsLobbyCodeKey =>
            PrefsLobbyCodePrefix + AuthenticationService.Instance.PlayerId;

        private static void RememberLobby(LobbyModel lobby)
        {
            if (lobby == null || string.IsNullOrEmpty(lobby.Id))
                return;

            PlayerPrefs.SetString(PrefsLobbyIdKey, lobby.Id);
            PlayerPrefs.SetString(PrefsLobbyCodeKey, lobby.LobbyCode ?? string.Empty);
            PlayerPrefs.Save();
        }

        private static void ClearRememberedLobby()
        {
            PlayerPrefs.DeleteKey(PrefsLobbyIdKey);
            PlayerPrefs.DeleteKey(PrefsLobbyCodeKey);
            PlayerPrefs.Save();
        }

        private async Task<LobbyModel> RecoverAlreadyJoinedLobbyAsync(
            string lobbyCode,
            JoinLobbyByCodeOptions joinOptions)
        {
            var fromCache = await TryReconnectCachedLobbyAsync(lobbyCode);
            if (fromCache != null)
                return fromCache;

            var fromJoined = await TryResolveJoinedLobbyByCodeAsync(lobbyCode);
            if (fromJoined != null)
                return fromJoined;

            await LeaveKnownLobbiesAsync();

            try
            {
                return await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);
            }
            catch (LobbyServiceException ex) when (IsAlreadyMemberError(ex))
            {
                var retryCache = await TryReconnectCachedLobbyAsync(lobbyCode);
                if (retryCache != null)
                    return retryCache;

                return await TryResolveJoinedLobbyByCodeAsync(lobbyCode);
            }
        }

        private async Task<LobbyModel> TryReconnectCachedLobbyAsync(string lobbyCode)
        {
            var cachedId = PlayerPrefs.GetString(PrefsLobbyIdKey, string.Empty);
            var cachedCode = PlayerPrefs.GetString(PrefsLobbyCodeKey, string.Empty);
            if (string.IsNullOrEmpty(cachedId) ||
                !string.Equals(cachedCode, lobbyCode, StringComparison.OrdinalIgnoreCase))
                return null;

            var lobby = await ReconnectOrGetLobbyAsync(cachedId, CurrentLobby);
            if (lobby == null ||
                !string.Equals(lobby.LobbyCode, lobbyCode, StringComparison.OrdinalIgnoreCase))
                return null;

            return lobby;
        }

        private async Task LeaveKnownLobbiesAsync(bool silent = false)
        {
            var localPlayerId = AuthenticationService.Instance.PlayerId;
            var lobbyIds = new HashSet<string>(StringComparer.Ordinal);

            if (CurrentLobby != null && !string.IsNullOrEmpty(CurrentLobby.Id))
                lobbyIds.Add(CurrentLobby.Id);

            var cachedId = PlayerPrefs.GetString(PrefsLobbyIdKey, string.Empty);
            if (!string.IsNullOrEmpty(cachedId))
                lobbyIds.Add(cachedId);

            foreach (var joinedId in await TryGetJoinedLobbyIdsAsync())
            {
                if (!string.IsNullOrEmpty(joinedId))
                    lobbyIds.Add(joinedId);
            }

            foreach (var lobbyId in lobbyIds)
            {
                try
                {
                    await Lobbies.Instance.RemovePlayerAsync(lobbyId, localPlayerId);
                }
                catch (Exception ex)
                {
                    if (!silent)
                        Debug.LogWarning($"Lobby leave uyarı ({lobbyId}): {ex.Message}");
                }
            }

            CurrentLobby = null;
            ClearRememberedLobby();
        }

        private static async Task<List<string>> TryGetJoinedLobbyIdsAsync()
        {
            try
            {
                var joinedIds = await Lobbies.Instance.GetJoinedLobbiesAsync();
                return joinedIds ?? new List<string>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GetJoinedLobbiesAsync başarısız: {ex.Message}");
                return new List<string>();
            }
        }

        private static async Task<LobbyModel> TryResolveJoinedLobbyByCodeAsync(string lobbyCode)
        {
            var joinedIds = await TryGetJoinedLobbyIdsAsync();

            if (joinedIds.Count == 0)
                return null;

            foreach (var lobbyId in joinedIds)
            {
                if (string.IsNullOrEmpty(lobbyId)) continue;

                var lobby = await ReconnectOrGetLobbyAsync(lobbyId);
                if (lobby == null)
                    continue;

                if (string.Equals(lobby.LobbyCode, lobbyCode, StringComparison.OrdinalIgnoreCase))
                    return lobby;
            }

            return null;
        }

        private static async Task<LobbyModel> ReconnectOrGetLobbyAsync(string lobbyId, LobbyModel fallback = null)
        {
            try
            {
                return await Lobbies.Instance.ReconnectToLobbyAsync(lobbyId);
            }
            catch (LobbyServiceException ex) when (IsAlreadyMemberError(ex))
            {
                if (fallback != null && string.Equals(fallback.Id, lobbyId, StringComparison.Ordinal))
                    return fallback;
            }
            catch (Exception)
            {
                // Reconnect failed; try fallback below.
            }

            return fallback != null && string.Equals(fallback.Id, lobbyId, StringComparison.Ordinal)
                ? fallback
                : null;
        }

        public async Task UpdateLocalPlayerSlotAsync(int slotIndex, string team, string role)
        {
            if (CurrentLobby == null)
                throw new InvalidOperationException("Lobby yok.");

            var playerId = AuthenticationService.Instance.PlayerId;
            var options = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { LobbyConstants.TeamKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, team) },
                    { LobbyConstants.RoleKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, role) },
                    { LobbyConstants.SlotKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, slotIndex.ToString()) },
                }
            };

            CurrentLobby = await Lobbies.Instance.UpdatePlayerAsync(CurrentLobby.Id, playerId, options);
        }

        public async Task UpdateLobbyDataAsync(Dictionary<string, DataObject> data)
        {
            if (CurrentLobby == null) return;

            try
            {
                var options = new UpdateLobbyOptions { Data = data };
                CurrentLobby = await Lobbies.Instance.UpdateLobbyAsync(CurrentLobby.Id, options);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lobby Update hata: {ex}");
                throw;
            }
        }

        public void StartHeartbeat(float intervalSeconds = 15f)
        {
            StopHeartbeat();
            if (CurrentLobby == null) return;

            _heartbeatCts = new CancellationTokenSource();
            _ = HeartbeatLoopAsync(CurrentLobby.Id, TimeSpan.FromSeconds(intervalSeconds), _heartbeatCts.Token);
        }

        public Player GetLocalPlayer()
        {
            if (CurrentLobby?.Players == null) return null;

            var localId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
            return CurrentLobby.Players.Find(p => p.Id == localId);
        }

        public void StopHeartbeat()
        {
            _heartbeatCts?.Cancel();
            _heartbeatCts?.Dispose();
            _heartbeatCts = null;
        }

        public void StartPolling(
            float intervalSeconds = 2f,
            Action<LobbyModel> onLobbyUpdated = null,
            Action onLobbyClosed = null)
        {
            StopPolling();
            if (CurrentLobby == null)
                return;

            _onLobbyClosed = onLobbyClosed;
            _pollCts = new CancellationTokenSource();
            _ = PollLoopAsync(CurrentLobby.Id, TimeSpan.FromSeconds(intervalSeconds), _pollCts.Token, onLobbyUpdated);
        }

        public async Task<LobbyModel> RefreshLobbyAsync()
        {
            if (CurrentLobby == null) return null;

            try
            {
                CurrentLobby = await Lobbies.Instance.GetLobbyAsync(CurrentLobby.Id);
                return CurrentLobby;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lobby Refresh hata: {ex}");
                throw;
            }
        }

        public void StopPolling()
        {
            _pollCts?.Cancel();
            _pollCts?.Dispose();
            _pollCts = null;
            _onLobbyClosed = null;
        }

        private static bool IsLobbyUnavailableError(LobbyServiceException ex)
        {
            if (ex.Reason is LobbyExceptionReason.LobbyNotFound or LobbyExceptionReason.Forbidden)
                return true;

            var msg = ex.Message ?? string.Empty;
            return msg.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0
                   || msg.IndexOf("does not exist", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void NotifyLobbyClosed()
        {
            var callback = _onLobbyClosed;
            _onLobbyClosed = null;
            callback?.Invoke();
        }

        private static async Task HeartbeatLoopAsync(string lobbyId, TimeSpan interval, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Lobby Heartbeat uyarı: {ex.Message}");
                }

                try
                {
                    await Task.Delay(interval, ct);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }

        private async Task PollLoopAsync(string lobbyId, TimeSpan interval, CancellationToken ct, Action<LobbyModel> onLobbyUpdated)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var lobby = await Lobbies.Instance.GetLobbyAsync(lobbyId);
                    CurrentLobby = lobby;

                    if (LobbyConstants.IsGameClosed(lobby))
                    {
                        CurrentLobby = null;
                        ClearRememberedLobby();
                        NotifyLobbyClosed();
                        return;
                    }

                    onLobbyUpdated?.Invoke(lobby);
                }
                catch (LobbyServiceException ex) when (IsLobbyUnavailableError(ex))
                {
                    CurrentLobby = null;
                    ClearRememberedLobby();
                    NotifyLobbyClosed();
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Lobby Poll uyarı: {ex.Message}");
                }

                try
                {
                    await Task.Delay(interval, ct);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }
    }
}

