using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoopPuzzle.Core.Bootstrap;
using CoopPuzzle.Gameplay.Core;
using CoopPuzzle.Gameplay.UI;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [Header("Start Game")]
        [SerializeField] private int minPlayersToStart = LobbyConstants.MinPlayersToStart;

        public int MinPlayersToStart => minPlayersToStart;

        public LobbyServiceFacade LobbyService { get; private set; }
        public RelayServiceFacade RelayService { get; private set; }

        public event Action<string> StatusChanged;
        public event Action LobbySessionEnded;

        private const float ReturnToMenuTimeoutSeconds = 12f;
        private const float HostShutdownGraceSeconds = 2.5f;

        private bool _gameStartHandled;
        private bool _matchEndPending;
        private Coroutine _returnToMenuRoutine;

        public bool IsLocalPlayerHost()
        {
            var lobby = LobbyService?.CurrentLobby;
            if (lobby == null) return false;
            return string.Equals(lobby.HostId, AuthenticationService.Instance.PlayerId, StringComparison.Ordinal);
        }

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

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

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
                    playerData: BuildPlayerData(playerName, slotIndex: 0)
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
                StartLobbyPolling(onLobbyUpdated);

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
                    StartLobbyPolling(onLobbyUpdated);
                    return;
                }

                if (alreadyInLobby && !networkActive)
                {
                    SetStatus("Lobiye yeniden bağlanılıyor...");
                    var existingLobby = LobbyService.CurrentLobby;
                    StartLobbyPolling(onLobbyUpdated);

                    if (existingLobby?.Data != null &&
                        existingLobby.Data.TryGetValue(LobbyConstants.RelayJoinCodeKey, out var existingRelay))
                    {
                        var existingRelayData = await RelayService.JoinRelayAsync(existingRelay.Value);
                        networkBootstrap.ConfigureRelay(existingRelayData);
                        networkBootstrap.StartClient();
                        return;
                    }
                }

                SetStatus("Lobby'ye katılınıyor...");
                var lobby = await LobbyService.JoinLobbyByCodeAsync(
                    normalizedCode,
                    BuildPlayerData(playerName, slotIndex: -1));

                if (!LobbyService.IsLocalPlayerInLobby())
                    throw new InvalidOperationException("Lobby'ye oyuncu olarak eklenilemedin.");

                if (IsLocalPlayerHost())
                {
                    throw new InvalidOperationException(
                        "Bu oyun örneği host oturumu ile aynı hesabı kullanıyor. Client test için ikinci bir Unity penceresi/build aç.");
                }

                StartLobbyPolling(onLobbyUpdated);

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

        public async Task AssignLocalPlayerToSlotAsync(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= LobbySlotLayout.SlotCount)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));

            await EnsureInitializedAsync();

            var lobby = LobbyService.CurrentLobby;
            if (lobby == null)
                throw new InvalidOperationException("Aktif lobby yok.");

            if (!LobbyService.IsLocalPlayerInLobby())
            {
                SetStatus("Lobby'de kayıtlı değilsin. Önce Bağlan ile katıl.");
                return;
            }

            var localId = AuthenticationService.Instance.PlayerId;
            if (LobbySlotLayout.IsSlotOccupiedByOther(lobby, slotIndex, localId))
            {
                SetStatus("Bu slot dolu. Boş bir slota bas.");
                return;
            }

            var (team, role) = LobbySlotLayout.GetTeamRoleForSlot(slotIndex);
            await LobbyService.UpdateLocalPlayerSlotAsync(slotIndex, team, role);
            SetStatus($"Takım slotu seçildi ({slotIndex + 1}/4).");
        }

        public async Task StartGameAsync()
        {
            SetStatus("Oyun başlatılıyor...");
            await EnsureInitializedAsync();

            var lobby = LobbyService.CurrentLobby;
            var required = Mathf.Clamp(minPlayersToStart, 1, LobbyConstants.MaxPlayers);
            if (lobby == null || lobby.Players == null || lobby.Players.Count < required)
            {
                SetStatus($"Oyun için en az {required} oyuncu gerekli ({lobby?.Players?.Count ?? 0}/{required}).");
                throw new InvalidOperationException("Lobby not ready");
            }

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

            _gameStartHandled = true;
            LobbyService.StopPolling();

            if (!networkBootstrap.LoadGameplayScene(GameplayScenes.Gameplay))
                throw new InvalidOperationException("Gameplay sahnesi yüklenemedi.");

            SetStatus("Oyun sahnesi yükleniyor...");
        }

        /// <summary>Lobby poll: host oyunu başlattığında client NGO sahne senkronunu takip eder.</summary>
        public void TryFollowGameStart(LobbyModel lobby)
        {
            if (lobby == null || !LobbyConstants.IsGameStarted(lobby))
                return;

            if (networkBootstrap != null && networkBootstrap.IsGameplaySceneActive())
                return;

            if (_gameStartHandled)
                return;

            _gameStartHandled = true;
            LobbyService?.StopPolling();

            if (IsLocalPlayerHost())
                return;

            if (networkBootstrap != null && networkBootstrap.IsConnected)
            {
                SetStatus("Host oyunu başlattı. Sahne yükleniyor (NGO senkron)...");
                return;
            }

            SetStatus("Oyun başladı ama Relay/NGO bağlantısı yok. Lobiye yeniden bağlan.");
        }

        public void MarkMatchEndPending() => _matchEndPending = true;

        public bool HasActiveLobbySession() =>
            LobbyService != null && LobbyService.HasActiveLobby();

        /// <summary>Host: lobiyi sil (herkes atılır). Client: lobiden ayrıl. NGO kapatılır.</summary>
        public async Task LeaveOrCloseLobbyAsync()
        {
            await EnsureInitializedAsync();

            _gameStartHandled = false;
            _matchEndPending = false;

            var hadLobby = LobbyService != null && LobbyService.HasActiveLobby();
            var isHost = hadLobby && IsLocalPlayerHost();

            if (hadLobby && isHost)
            {
                SetStatus("Lobi kapatılıyor, oyuncular çıkarılıyor...");
                try
                {
                    await LobbyService.UpdateLobbyDataAsync(new Dictionary<string, DataObject>
                    {
                        {
                            LobbyConstants.GameStateKey,
                            new DataObject(DataObject.VisibilityOptions.Public, LobbyConstants.GameStateClosed)
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Lobby] Kapatma durumu yazılamadı: {ex.Message}");
                }

                await LobbyService.DeleteCurrentLobbyAsync();
            }
            else if (hadLobby)
            {
                SetStatus("Lobiden ayrılıyor...");
                await LobbyService.LeaveCurrentLobbyAsync();
            }
            else
            {
                LobbyService?.Shutdown();
            }

            NetworkConnectionRegistry.Clear();
            networkBootstrap?.ShutdownNetwork();

            SetStatus(isHost ? "Lobi kapatıldı." : hadLobby ? "Lobiden ayrıldın." : "Ana menü.");
            LobbySessionEnded?.Invoke();
        }

        private void StartLobbyPolling(Action<LobbyModel> onLobbyUpdated)
        {
            LobbyService?.StartPolling(
                onLobbyUpdated: onLobbyUpdated,
                onLobbyClosed: HandleLobbyClosedRemotely);
        }

        private void HandleLobbyClosedRemotely()
        {
            if (IsLocalPlayerHost())
                return;

            _gameStartHandled = false;
            _matchEndPending = false;

            NetworkConnectionRegistry.Clear();
            networkBootstrap?.ShutdownNetwork();

            SetStatus("Host lobiyi kapattı.");
            LobbySessionEnded?.Invoke();
        }

        /// <summary>Tüm oyuncular: menü sahnesini bekle, sonra lobby/NGO kapat.</summary>
        public void BeginReturnToMenu()
        {
            if (_returnToMenuRoutine != null)
                StopCoroutine(_returnToMenuRoutine);

            MarkMatchEndPending();
            _returnToMenuRoutine = StartCoroutine(ReturnToMenuRoutine());
        }

        /// <summary>Host: lobby finished + NGO ile menü sahnesini yükle.</summary>
        public async Task EndMatchReturnToLobbyAsHostAsync()
        {
            if (networkBootstrap == null)
                networkBootstrap = FindFirstObjectByType<NetworkBootstrap>();

            var isHost = networkBootstrap != null && networkBootstrap.IsHostOrServer;
            if (!isHost)
                return;

            _gameStartHandled = false;
            SetStatus("Maç bitti. Ana menüye dönülüyor...");

            try
            {
                if (LobbyService?.CurrentLobby != null)
                {
                    await LobbyService.UpdateLobbyDataAsync(new Dictionary<string, DataObject>
                    {
                        {
                            LobbyConstants.GameStateKey,
                            new DataObject(DataObject.VisibilityOptions.Public, LobbyConstants.GameStateFinished)
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Lobby] Maç bitiş lobby güncellemesi: {ex.Message}");
            }

            LobbyService?.StopPolling();
            LobbyService?.StopHeartbeat();
            StartLobbyPolling(lobby =>
            {
                TryFollowGameEnd(lobby);
                TryFollowLobbyClosed(lobby);
            });

            if (networkBootstrap != null && networkBootstrap.IsConnected)
            {
                if (!networkBootstrap.LoadLobbyScene())
                    Debug.LogWarning("[Lobby] NGO ile menü sahnesi yüklenemedi.");
            }
            else
            {
                SceneManager.LoadScene(GameplayScenes.Lobby, LoadSceneMode.Single);
            }
        }

        /// <summary>Lobby poll: host lobiyi kapattı.</summary>
        public void TryFollowLobbyClosed(LobbyModel lobby)
        {
            if (lobby == null || !LobbyConstants.IsGameClosed(lobby))
                return;

            if (IsLocalPlayerHost())
                return;

            HandleLobbyClosedRemotely();
        }

        /// <summary>Lobby poll: host maçı bitirdiğinde client yedek senkron.</summary>
        public void TryFollowGameEnd(LobbyModel lobby)
        {
            if (lobby == null || !LobbyConstants.IsGameFinished(lobby))
                return;

            if (networkBootstrap != null && networkBootstrap.IsLobbySceneActive())
            {
                if (_matchEndPending && _returnToMenuRoutine == null)
                    BeginReturnToMenu();
                return;
            }

            if (IsLocalPlayerHost())
                return;

            BeginReturnToMenu();
        }

        private IEnumerator ReturnToMenuRoutine()
        {
            SetStatus("Ana menüye dönülüyor...");

            if (networkBootstrap == null)
                networkBootstrap = FindFirstObjectByType<NetworkBootstrap>();

            var deadline = Time.time + ReturnToMenuTimeoutSeconds;
            while (Time.time < deadline)
            {
                if (networkBootstrap != null && networkBootstrap.IsLobbySceneActive())
                    break;

                yield return new WaitForSeconds(0.2f);
            }

            if (networkBootstrap == null || !networkBootstrap.IsLobbySceneActive())
            {
                Debug.LogWarning("[Lobby] Menü sahnesi NGO ile gelmedi; yerel yükleme.");
                SceneManager.LoadScene(GameplayScenes.Lobby, LoadSceneMode.Single);
                yield return null;
            }

            var isHost = networkBootstrap != null && networkBootstrap.IsHostOrServer;
            if (isHost)
                yield return new WaitForSeconds(HostShutdownGraceSeconds);

            var finalizeTask = FinalizeMatchEndLocallyAsync();
            while (!finalizeTask.IsCompleted)
                yield return null;

            _returnToMenuRoutine = null;
        }

        public async Task FinalizeMatchEndLocallyAsync()
        {
            _gameStartHandled = false;
            _matchEndPending = false;

            LobbyService?.Shutdown();
            NetworkConnectionRegistry.Clear();

            if (networkBootstrap != null)
                networkBootstrap.ShutdownNetwork();

            GameplayWinUI.Instance?.Hide();
            SetStatus("Ana menüye döndün.");
            await Task.CompletedTask;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_matchEndPending || !IsLobbySceneName(scene.name))
                return;

            if (_returnToMenuRoutine == null)
                BeginReturnToMenu();
        }

        private static bool IsLobbySceneName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return false;

            return string.Equals(sceneName, GameplayScenes.Lobby, StringComparison.OrdinalIgnoreCase)
                   || sceneName.IndexOf("men", StringComparison.OrdinalIgnoreCase) >= 0;
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

        private static Dictionary<string, PlayerDataObject> BuildPlayerData(string playerName, int slotIndex = -1)
        {
            playerName = string.IsNullOrWhiteSpace(playerName) ? "Oyuncu" : playerName.Trim();

            string team = string.Empty;
            string role = string.Empty;
            string slot = string.Empty;

            if (slotIndex is >= 0 and < LobbySlotLayout.SlotCount)
            {
                var tr = LobbySlotLayout.GetTeamRoleForSlot(slotIndex);
                team = tr.team;
                role = tr.role;
                slot = slotIndex.ToString();
            }

            return new Dictionary<string, PlayerDataObject>
            {
                { LobbyConstants.PlayerNameKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                { LobbyConstants.TeamKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, team) },
                { LobbyConstants.RoleKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, role) },
                { LobbyConstants.SlotKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, slot) },
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

