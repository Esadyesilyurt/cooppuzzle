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

    private string _currentRoomCode;
    private const int RoomCodeLength = 4;
    private const string RoomCodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private void Awake()
    {
        EnsureArraySize();
    }

    public void HostLobby()
    {
        _currentRoomCode = GenerateRoomCode(RoomCodeLength);

        if (hostRoomCodeText != null)
            hostRoomCodeText.text = _currentRoomCode;

        ResetHostPlayerList();

        if (hostPlayerNameTexts != null && hostPlayerNameTexts.Length > 0 && hostPlayerNameTexts[0] != null)
            hostPlayerNameTexts[0].text = hostPlayerDefaultName;
    }

    public void JoinLobby()
    {
        var code = (joinRoomCodeInput != null ? joinRoomCodeInput.text : string.Empty).Trim().ToUpperInvariant();
        var playerName = (joinPlayerNameInput != null ? joinPlayerNameInput.text : string.Empty).Trim();

        if (string.IsNullOrEmpty(_currentRoomCode))
        {
            Debug.Log("Önce HostLobby() ile oda oluşturulmalı (mock).");
            return;
        }

        if (!string.Equals(code, _currentRoomCode, StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("Hatalı Kod");
            return;
        }

        if (string.IsNullOrWhiteSpace(playerName))
            playerName = "Oyuncu";

        var slot = FindNextAvailableSlotIndex();
        if (slot < 0)
        {
            Debug.Log("Lobi dolu (mock).");
            return;
        }

        if (hostPlayerNameTexts[slot] != null)
            hostPlayerNameTexts[slot].text = playerName;
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
