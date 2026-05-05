using System;
using TMPro;
using UnityEngine;

public class GameLobbyController : MonoBehaviour
{
    [Header("Host UI")]
    public TextMeshProUGUI hostRoomCodeText;
    public TextMeshProUGUI[] hostPlayerNameTexts = new TextMeshProUGUI[4];

    [Header("Join UI")]
    public TMP_InputField joinRoomCodeInput;
    public TMP_InputField joinPlayerNameInput;

    [Header("Mock/Defaults")]
    [SerializeField] private string hostPlayerDefaultName = "Host Oyuncu";
    [SerializeField] private string waitingText = "Bekleniyor...";

    private const int RoomCodeLength = 4;
    private const string RoomCodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    [Header("Networking (NGO + UGS)")]
    [SerializeField] private CoopPuzzle.Lobby.LobbyCoordinator lobbyCoordinator;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Awake()
    {
        EnsureArraySize();

        if (lobbyCoordinator == null)
            lobbyCoordinator = FindFirstObjectByType<CoopPuzzle.Lobby.LobbyCoordinator>();
    }

    public void HostLobby()
    {
        ResetHostPlayerList();

        if (hostPlayerNameTexts != null && hostPlayerNameTexts.Length > 0 && hostPlayerNameTexts[0] != null)
            hostPlayerNameTexts[0].text = hostPlayerDefaultName;

        if (lobbyCoordinator == null)
        {
            SetStatus("LobbyCoordinator bulunamadı. Sahneye eklemeliyiz.");
            return;
        }

        _ = lobbyCoordinator.HostAsync(
            playerName: hostPlayerDefaultName,
            onLobbyUpdated: lobby =>
            {
                if (hostRoomCodeText != null)
                    hostRoomCodeText.text = lobby.LobbyCode;
            }
        );
    }

    public void JoinLobby()
    {
        var code = (joinRoomCodeInput != null ? joinRoomCodeInput.text : string.Empty).Trim().ToUpperInvariant();
        var playerName = (joinPlayerNameInput != null ? joinPlayerNameInput.text : string.Empty).Trim();

        if (lobbyCoordinator == null)
        {
            SetStatus("LobbyCoordinator bulunamadı. Sahneye eklemeliyiz.");
            return;
        }

        if (string.IsNullOrEmpty(code) || code.Length < RoomCodeLength)
        {
            SetStatus("Geçersiz oda kodu.");
            return;
        }

        if (string.IsNullOrWhiteSpace(playerName))
            playerName = "Oyuncu";

        _ = lobbyCoordinator.JoinAsync(
            lobbyCode: code,
            playerName: playerName,
            onLobbyUpdated: lobby =>
            {
                if (hostPlayerNameTexts == null) return;
                if (lobby.Players == null) return;

                ResetHostPlayerList();
                for (int i = 0; i < Mathf.Min(hostPlayerNameTexts.Length, lobby.Players.Count); i++)
                {
                    var t = hostPlayerNameTexts[i];
                    if (t == null) continue;

                    var p = lobby.Players[i];
                    if (p?.Data != null && p.Data.TryGetValue(CoopPuzzle.Lobby.LobbyConstants.PlayerNameKey, out var nameObj))
                        t.text = nameObj.Value;
                    else
                        t.text = "Oyuncu";
                }
            }
        );
    }

    private void ResetHostPlayerList()
    {
        if (hostPlayerNameTexts == null) return;

        for (int i = 0; i < hostPlayerNameTexts.Length; i++)
        {
            if (hostPlayerNameTexts[i] != null)
                hostPlayerNameTexts[i].text = waitingText;
        }
    }

    private int FindNextAvailableSlotIndex()
    {
        if (hostPlayerNameTexts == null) return -1;

        for (int i = 1; i < hostPlayerNameTexts.Length; i++)
        {
            var t = hostPlayerNameTexts[i];
            if (t == null) continue;

            var current = (t.text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(current) || string.Equals(current, waitingText, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static string GenerateRoomCode(int length)
    {
        if (length <= 0) return string.Empty;

        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = RoomCodeAlphabet[UnityEngine.Random.Range(0, RoomCodeAlphabet.Length)];
        }

        return new string(chars);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        else
            Debug.Log(message);
    }

    private void EnsureArraySize()
    {
        if (hostPlayerNameTexts == null)
        {
            hostPlayerNameTexts = new TextMeshProUGUI[4];
            return;
        }

        if (hostPlayerNameTexts.Length != 4)
        {
            Array.Resize(ref hostPlayerNameTexts, 4);
        }
    }
}
