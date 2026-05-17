using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace CoopPuzzle.Lobby
{
    using LobbyModel = Unity.Services.Lobbies.Models.Lobby;

    public sealed class LobbyServiceFacade
    {
        public LobbyModel CurrentLobby { get; private set; }

        private CancellationTokenSource _pollCts;
        private CancellationTokenSource _heartbeatCts;

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
            try
            {
                var options = new JoinLobbyByCodeOptions
                {
                    Player = new Player { Data = playerData }
                };

                CurrentLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
                return CurrentLobby;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lobby JoinByCode hata: {ex}");
                throw;
            }
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

        public void StartPolling(float intervalSeconds = 2f, Action<LobbyModel> onLobbyUpdated = null)
        {
            StopPolling();
            if (CurrentLobby == null) return;

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
                    onLobbyUpdated?.Invoke(lobby);
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

