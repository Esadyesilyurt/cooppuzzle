using System;
using CoopPuzzle.Lobby;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

/// <summary>HostPanel: kod, 4 takım slotu, başlat/geri.</summary>
public sealed class HostPanelController : MonoBehaviour
{
    private static readonly Color Team1Color = new(1f, 0f, 0.07f, 1f);
    private static readonly Color Team2Color = new(0f, 0.35f, 1f, 1f);
    private static readonly Color EmptySlotColor = new(1f, 1f, 1f, 0.45f);

    [SerializeField] private GameLobbyController gameLobby;
    [SerializeField] private LobbyCoordinator lobbyCoordinator;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button backButton;
    [SerializeField] private string[] slotObjectNames = { "isim1", "isim2", "isim3", "isim4" };

    private readonly Button[] _slotButtons = new Button[LobbySlotLayout.SlotCount];
    private readonly TextMeshProUGUI[] _slotLabels = new TextMeshProUGUI[LobbySlotLayout.SlotCount];
    private bool _wired;
    private bool _clientView;

    private void Awake()
    {
        WireIfNeeded();
    }

    private void OnEnable()
    {
        WireIfNeeded();
    }

    public void WireIfNeeded()
    {
        if (_wired) return;

        if (gameLobby == null)
            gameLobby = FindAnyObjectByType<GameLobbyController>();
        if (lobbyCoordinator == null)
            lobbyCoordinator = FindAnyObjectByType<LobbyCoordinator>();

        ResolveSlotWidgets();
        ResolveStartButton();
        ResolveBackButton();

        for (int i = 0; i < LobbySlotLayout.SlotCount; i++)
        {
            var index = i;
            if (_slotButtons[i] == null) continue;
            _slotButtons[i].onClick.RemoveAllListeners();
            _slotButtons[i].onClick.AddListener(() => OnSlotClicked(index));
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(() => gameLobby?.StartGame());
            startGameButton.interactable = false;
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => gameLobby?.ReturnToLobbyPanel());
        }

        ApplyRoomModeUi();
        _wired = true;
    }

    public void SetClientView(bool isClient)
    {
        _clientView = isClient;
        ApplyRoomModeUi();
    }

    public void SetHostView() => SetClientView(false);

    private void ApplyRoomModeUi()
    {
        if (startGameButton == null) return;

        if (_clientView)
        {
            startGameButton.gameObject.SetActive(false);
            startGameButton.interactable = false;
            SetSlotsInteractable(IsLocalPlayerInLobby());
            return;
        }

        startGameButton.gameObject.SetActive(true);
        startGameButton.interactable = false;
    }

    public void RefreshFromLobby(Lobby lobby, string waitingText)
    {
        WireIfNeeded();

        for (int i = 0; i < LobbySlotLayout.SlotCount; i++)
        {
            var label = _slotLabels[i];
            if (label == null) continue;

            label.text = waitingText;
            label.color = EmptySlotColor;
            if (_slotButtons[i] != null)
                _slotButtons[i].interactable = true;
        }

        if (lobby?.Players == null)
        {
            RefreshStartButton(0);
            SetSlotsInteractable(false);
            return;
        }

        var localInLobby = IsLocalPlayerInLobby(lobby);
        var localId = AuthenticationService.Instance.PlayerId;

        foreach (var player in lobby.Players)
        {
            var slot = LobbySlotLayout.GetSlotIndexForPlayer(player);
            if (slot < 0 || slot >= LobbySlotLayout.SlotCount) continue;

            var label = _slotLabels[slot];
            if (label == null) continue;

            if (player.Data != null && player.Data.TryGetValue(LobbyConstants.PlayerNameKey, out var nameObj)
                && !string.IsNullOrWhiteSpace(nameObj.Value))
                label.text = nameObj.Value;
            else
                label.text = "Oyuncu";

            label.color = slot < 2 ? Team1Color : Team2Color;

            if (_slotButtons[slot] != null)
                _slotButtons[slot].interactable = false;
        }

        for (int i = 0; i < LobbySlotLayout.SlotCount; i++)
        {
            if (_slotButtons[i] == null)
                continue;

            var occupiedByOther = LobbySlotLayout.IsSlotOccupiedByOther(lobby, i, localId);
            _slotButtons[i].interactable = localInLobby && !occupiedByOther;
        }

        if (!localInLobby)
            SetSlotsInteractable(false);

        RefreshStartButton(lobby.Players.Count);
    }

    private bool IsLocalPlayerInLobby(Lobby lobby = null)
    {
        lobby ??= lobbyCoordinator?.LobbyService?.CurrentLobby;
        if (lobby?.Players == null)
            return false;

        var localId = AuthenticationService.Instance.PlayerId;
        foreach (var p in lobby.Players)
        {
            if (p != null && p.Id == localId)
                return true;
        }

        return false;
    }

    private void SetSlotsInteractable(bool interactable)
    {
        for (int i = 0; i < _slotButtons.Length; i++)
        {
            if (_slotButtons[i] != null)
                _slotButtons[i].interactable = interactable;
        }
    }

    private async void OnSlotClicked(int slotIndex)
    {
        if (lobbyCoordinator == null) return;

        var lobby = lobbyCoordinator.LobbyService?.CurrentLobby;
        if (lobby == null)
        {
            gameLobby?.SetStatusPublic("Önce lobi kurulmalı.");
            return;
        }

        if (!IsLocalPlayerInLobby(lobby))
        {
            gameLobby?.SetStatusPublic("Lobby'ye katılmadan slot seçemezsin.");
            return;
        }

        var localId = AuthenticationService.Instance.PlayerId;
        if (LobbySlotLayout.IsSlotOccupiedByOther(lobby, slotIndex, localId))
        {
            gameLobby?.SetStatusPublic("Bu slot dolu. Boş bir slota bas.");
            return;
        }

        try
        {
            await lobbyCoordinator.AssignLocalPlayerToSlotAsync(slotIndex);
        }
        catch (Exception ex)
        {
            gameLobby?.SetStatusPublic($"Slot seçilemedi: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    private void RefreshStartButton(int playerCount)
    {
        if (startGameButton == null || _clientView) return;

        var isHost = lobbyCoordinator != null && lobbyCoordinator.IsLocalPlayerHost();
        startGameButton.gameObject.SetActive(isHost);
        if (!isHost)
        {
            startGameButton.interactable = false;
            return;
        }

        var required = lobbyCoordinator != null
            ? lobbyCoordinator.MinPlayersToStart
            : LobbyConstants.MinPlayersToStart;
        startGameButton.interactable = playerCount >= required;
    }

    private void ResolveStartButton()
    {
        if (startGameButton != null) return;
        foreach (var b in GetComponentsInChildren<Button>(true))
        {
            if (b == null) continue;
            var n = b.gameObject.name ?? string.Empty;
            if (n.Contains("Baslat", StringComparison.OrdinalIgnoreCase)
                || n.Contains("Başlat", StringComparison.OrdinalIgnoreCase)
                || n.Contains("Start", StringComparison.OrdinalIgnoreCase))
            {
                startGameButton = b;
                return;
            }
        }
    }

    private void ResolveBackButton()
    {
        if (backButton != null) return;
        foreach (var b in GetComponentsInChildren<Button>(true))
        {
            if (b == null) continue;
            var n = b.gameObject.name ?? string.Empty;
            if (n.Contains("Geri", StringComparison.OrdinalIgnoreCase)
                || (n.Contains("Back", StringComparison.OrdinalIgnoreCase)
                    && !n.Contains("Background", StringComparison.OrdinalIgnoreCase)))
            {
                backButton = b;
                return;
            }
        }
    }

    private void ResolveSlotWidgets()
    {
        EnsureSlotNames();

        for (int i = 0; i < LobbySlotLayout.SlotCount; i++)
        {
            var slotName = slotObjectNames[i];
            var go = FindChildByName(slotName);
            if (go == null && gameLobby?.hostPlayerNameTexts != null
                && i < gameLobby.hostPlayerNameTexts.Length
                && gameLobby.hostPlayerNameTexts[i] != null)
                go = gameLobby.hostPlayerNameTexts[i].gameObject;

            if (go == null)
                continue;

            _slotLabels[i] = go.GetComponent<TextMeshProUGUI>();
            if (_slotLabels[i] == null)
                continue;

            _slotLabels[i].raycastTarget = true;
            _slotButtons[i] = go.GetComponent<Button>();
            if (_slotButtons[i] == null)
                _slotButtons[i] = go.AddComponent<Button>();

            // TMP is a Graphic — no separate Image needed (AddComponent<Image> was null on TMP-only GO).
            _slotButtons[i].targetGraphic = _slotLabels[i];
        }
    }

    private void EnsureSlotNames()
    {
        if (slotObjectNames != null && slotObjectNames.Length >= LobbySlotLayout.SlotCount)
            return;

        slotObjectNames = new[] { "isim1", "isim2", "isim3", "isim4" };
    }

    private GameObject FindChildByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t != null && string.Equals(t.gameObject.name, objectName, StringComparison.OrdinalIgnoreCase))
                return t.gameObject;
        }

        return null;
    }
}
