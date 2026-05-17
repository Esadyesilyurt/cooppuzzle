using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoopPuzzle.Core.Bootstrap;
using CoopPuzzle.Gameplay.Core;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace CoopPuzzle.Lobby
{
    using LobbyModel = Unity.Services.Lobbies.Models.Lobby;

    public sealed class LobbyCoordinator : MonoBehaviour
    {
        public static LobbyCoordinator Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private UgsInitializer ugsInitializer;
        [SerializeField] private NetworkBootstrap networkBootstrap;

        [Header("Optional UI Output")]
        [SerializeField] private TextMeshProUGUI statusText;

        public LobbyServiceFacade LobbyService { get; private set; }
        public RelayServiceFacade RelayService { get; private set; }

        public event Action<string> StatusChanged;

        private void Reset()
        {
            ugsInitializer = FindFirstObjectByType<UgsInitializer>();
            networkBootstrap = FindFirstObjectByType<NetworkBootstrap>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LobbyService ??= new LobbyServiceFacade();
            RelayService ??= new RelayServiceFacade();
        }

        private void OnApplicationQuit() => ShutdownLobbySession();

        private void OnDestroy()
        {
            ShutdownLobbySession();
            if (Instance == this)
                Instance = null;
        }

        private void ShutdownLobbySession()
        {
            LobbyService?.Shutdown();
        }

        public async Task HostAsync(string playerName, Action<LobbyModel> onLobbyUpdated = null)
        {
            try
            {
                SetStatus("Servisler hazırlanıyor...");
                await EnsureInitializedAsync();

                SetStatus("Relay oluşturuluyor...");
                var (relayJoinCode, relayServerData) = await RelayService.CreateRelayAsync(maxConnections: LobbyConstants.MaxPlayers - 1);

                SetStatus("Lobby oluşturuluyor...");
                var lobby = await LobbyService.CreateLobbyAsync(
                    lobbyName: "CoopPuzzleLobby",
                    maxPlayers: LobbyConstants.MaxPlayers,
                    playerData: BuildPlayerData(playerName)
                );

                SetStatus("Lobby verisi güncelleniyor...");
                await LobbyService.UpdateLobbyDataAsync(new Dictionary<string, DataObject>
                {
                    {
                        LobbyConstants.RelayJoinCodeKey,
                        new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
                    }
                });

                LobbyService.StartHeartbeat();
                LobbyService.StartPolling(onLobbyUpdated: onLobbyUpdated);

                SetStatus($"Host hazır. LobbyCode: {lobby.LobbyCode}");

                networkBootstrap.ConfigureRelay(relayServerData);
                networkBootstrap.StartHost();
            }
            catch (Exception ex)
            {
                SetStatus($"Lobi kurulamadı: {ex.Message}");
                Debug.LogException(ex);
                throw;
            }
        }

        public async Task JoinAsync(string lobbyCode, string playerName, Action<LobbyModel> onLobbyUpdated = null)
        {
            try
            {
                SetStatus("Servisler hazırlanıyor...");
                await EnsureInitializedAsync();

                var normalizedCode = LobbyConstants.NormalizeLobbyCode(lobbyCode);
                var alreadyInLobby = LobbyService.CurrentLobby != null &&
                    string.Equals(LobbyService.CurrentLobby.LobbyCode, normalizedCode, StringComparison.OrdinalIgnoreCase);
                var networkActive = networkBootstrap != null && networkBootstrap.IsConnected;

                if (alreadyInLobby && networkActive)
                {
                    SetStatus($"Zaten bu lobidesin ({normalizedCode}).");
                    LobbyService.StartPolling(onLobbyUpdated: onLobbyUpdated);
                    return;
                }

                SetStatus("Lobby'ye katılınıyor...");
                var lobby = await LobbyService.JoinLobbyByCodeAsync(normalizedCode, BuildPlayerData(playerName));

                LobbyService.StartPolling(onLobbyUpdated: onLobbyUpdated);

                if (lobby.Data == null || !lobby.Data.TryGetValue(LobbyConstants.RelayJoinCodeKey, out var relayCodeObj))
                    throw new InvalidOperationException("Lobby içinde Relay join code bulunamadı.");

                var relayJoinCode = relayCodeObj.Value;

                SetStatus("Relay'e bağlanılıyor...");
                var relayServerData = await RelayService.JoinRelayAsync(relayJoinCode);

                SetStatus("Client başlatılıyor...");
                networkBootstrap.ConfigureRelay(relayServerData);
                networkBootstrap.StartClient();
            }
            catch (Exception ex)
            {
                SetStatus($"Katılım başarısız: {ex.Message}");
                Debug.LogException(ex);
                throw;
            }
        }

        public async Task StartGameAsync()
        {
            SetStatus("Oyun başlatılıyor...");
            await EnsureInitializedAsync();

            if (networkBootstrap == null || !networkBootstrap.IsHostOrServer)
            {
                SetStatus("Oyunu yalnızca host başlatabilir.");
                throw new InvalidOperationException("Not host");
            }

            await LobbyService.UpdateLobbyDataAsync(new Dictionary<string, DataObject>
            {
                {
                    LobbyConstants.GameStateKey,
                    new DataObject(DataObject.VisibilityOptions.Public, LobbyConstants.GameStateStarted)
                }
            });

            if (!networkBootstrap.LoadGameplayScene(GameplayScenes.Gameplay))
                throw new InvalidOperationException("Gameplay sahnesi yüklenemedi.");

            SetStatus("Oyun sahnesi yükleniyor...");
        }

        private async Task EnsureInitializedAsync()
        {
            if (ugsInitializer == null)
                ugsInitializer = FindFirstObjectByType<UgsInitializer>();

            if (networkBootstrap == null)
                networkBootstrap = FindFirstObjectByType<NetworkBootstrap>();

            if (ugsInitializer == null)
                throw new InvalidOperationException("UgsInitializer sahnede yok. Bir GameObject'e ekleyip sahnede bulundurmalıyız.");

            if (networkBootstrap == null)
                throw new InvalidOperationException("NetworkBootstrap sahnede yok. NetworkManager + UnityTransport ile birlikte sahnede olmalı.");

            await ugsInitializer.InitializeIfNeededAsync();
        }

        private static Dictionary<string, PlayerDataObject> BuildPlayerData(string playerName)
        {
            playerName = string.IsNullOrWhiteSpace(playerName) ? "Oyuncu" : playerName.Trim();

            return new Dictionary<string, PlayerDataObject>
            {
                { LobbyConstants.PlayerNameKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                { LobbyConstants.TeamKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, string.Empty) },
                { LobbyConstants.RoleKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, string.Empty) },
                { LobbyConstants.ReadyKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0") },
            };
        }

        private void SetStatus(string message)
        {
            StatusChanged?.Invoke(message);

            if (statusText != null)
                statusText.text = message;

            Debug.Log($"[Lobby] {message}");
        }
    }
}

