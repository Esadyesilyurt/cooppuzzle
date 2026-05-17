using System;
using CoopPuzzle.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private string joinPlayerDefaultName = "Oyuncu";
    [SerializeField] private string waitingText = "Bekleniyor...";

    [Header("Networking (NGO + UGS)")]
    [SerializeField] private LobbyCoordinator lobbyCoordinator;
    [SerializeField] private LobbyUIManager lobbyUi;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button joinButton;

    private bool _joinInputsConfigured;
    private bool _isJoining;
    private string _lastHostCodeLogged;
    private TextMeshProUGUI _runtimeStatusText;

    private void Awake()
    {
        EnsureArraySize();
        EnsureMainMenuLobbyCodeInput();
        ResolveAllReferences();
        ConfigureJoinInputs();
        ResolveJoinButton();
        WireMainMenuJoinButton();
        EnsureReadableHostCodeStyle();
        EnsureReadableJoinInputStyle();

        if (lobbyCoordinator != null)
            lobbyCoordinator.StatusChanged += SetStatus;
    }

    private void OnDestroy()
    {
        if (lobbyCoordinator != null)
            lobbyCoordinator.StatusChanged -= SetStatus;

        UnbindJoinInputs();
    }

    public void HostLobby()
    {
        PrepareHostPanel();
        ResetHostPlayerList();

        if (hostPlayerNameTexts != null && hostPlayerNameTexts.Length > 0 && hostPlayerNameTexts[0] != null)
            hostPlayerNameTexts[0].text = hostPlayerDefaultName;

        if (lobbyCoordinator == null)
        {
            SetStatus("LobbyCoordinator bulunamadı. Tools > CoopPuzzle > Setup > Setup Lobby Scene çalıştır.");
            return;
        }

        _ = RunHostAsync();
    }

    public void PrepareHostPanel()
    {
        ResolveAllReferences();
        lobbyUi?.OpenHostPanel();
        ShowHostCode("------", loading: true);
        SetStatus("Lobi kuruluyor, kod birazdan görünecek...");
    }

    public void StartGame()
    {
        if (lobbyCoordinator == null)
        {
            SetStatus("LobbyCoordinator bulunamadı.");
            return;
        }

        _ = RunStartGameAsync();
    }

    public void PrepareJoinPanel()
    {
        ResolveAllReferences();
        lobbyUi?.OpenJoinPanel();
        ConfigureJoinInputs();

        if (joinRoomCodeInput != null)
        {
            joinRoomCodeInput.text = LobbyConstants.NormalizeLobbyCode(joinRoomCodeInput.text);
            joinRoomCodeInput.ActivateInputField();
            joinRoomCodeInput.Select();
            EnsureReadableJoinInputStyle();
        }

        if (joinPlayerNameInput != null && string.IsNullOrWhiteSpace(joinPlayerNameInput.text))
            joinPlayerNameInput.text = joinPlayerDefaultName;

        RefreshJoinButtonState();
        SetStatus($"OdaKodu alanına host'un {LobbyConstants.LobbyCodeLength} haneli kodunu yaz, sonra Bağlan.");
    }

    public void JoinLobby() => TryJoinLobby();

    public void OnJoinCodeSubmitted(string _) => TryJoinLobby();

    public void OnJoinCodeChanged(string value)
    {
        if (joinRoomCodeInput == null) return;

        var normalized = LobbyConstants.NormalizeLobbyCode(value);
        if (!string.Equals(joinRoomCodeInput.text, normalized, StringComparison.Ordinal))
        {
            joinRoomCodeInput.SetTextWithoutNotify(normalized);
            joinRoomCodeInput.caretPosition = normalized.Length;
        }

        RefreshJoinButtonState();
    }

    public void OnJoinPlayerNameChanged(string _) => RefreshJoinButtonState();

    public void PasteLobbyCodeFromClipboard()
    {
        var clip = GUIUtility.systemCopyBuffer;
        if (string.IsNullOrWhiteSpace(clip))
        {
            SetStatus("Panoda kopyalanmış kod yok.");
            return;
        }

        if (joinRoomCodeInput == null)
        {
            SetStatus("OdaKodu alanı bulunamadı. Önce 'lobi bağlan' menüsünü aç.");
            return;
        }

        joinRoomCodeInput.text = LobbyConstants.NormalizeLobbyCode(clip);
        RefreshJoinButtonState();
        SetStatus("Kod yapıştırıldı. Bağlan'a bas.");
    }

    private void TryJoinLobby()
    {
        if (_isJoining)
        {
            SetStatus("Zaten katılım deneniyor...");
            return;
        }

        ResolveAllReferences();
        lobbyUi?.OpenJoinPanel();

        if (lobbyCoordinator == null)
        {
            SetStatus("LobbyCoordinator bulunamadı.");
            return;
        }

        if (joinRoomCodeInput == null)
        {
            SetStatus("OdaKodu giriş alanı yok. 'lobi bağlan' ile JoinPanel'i aç.");
            return;
        }

        if (!LobbyConstants.TryValidateLobbyCode(joinRoomCodeInput.text, out var code, out var validationError))
        {
            SetStatus(validationError);
            return;
        }

        var playerName = joinPlayerNameInput != null
            ? joinPlayerNameInput.text.Trim()
            : string.Empty;
        if (string.IsNullOrWhiteSpace(playerName))
            playerName = joinPlayerDefaultName;

        joinRoomCodeInput.text = code;
        _ = RunJoinAsync(code, playerName);
    }

    private async System.Threading.Tasks.Task RunHostAsync()
    {
        try
        {
            await lobbyCoordinator.HostAsync(
                playerName: hostPlayerDefaultName,
                onLobbyUpdated: lobby =>
                {
                    ShowHostCode(lobby.LobbyCode, loading: false);
                    if (!string.Equals(_lastHostCodeLogged, lobby.LobbyCode, StringComparison.Ordinal))
                    {
                        _lastHostCodeLogged = lobby.LobbyCode;
                        SetStatus($"Lobi hazır! Kod: {lobby.LobbyCode}");
                    }
                }
            );
        }
        catch (Exception ex)
        {
            ShowHostCode("HATA", loading: false);
            SetStatus($"Lobi kurulamadı: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    private async System.Threading.Tasks.Task RunStartGameAsync()
    {
        try
        {
            await lobbyCoordinator.StartGameAsync();
            SetStatus("Oyun başlatıldı.");
        }
        catch (Exception ex)
        {
            SetStatus($"Oyun başlatılamadı: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    private async System.Threading.Tasks.Task RunJoinAsync(string code, string playerName)
    {
        _isJoining = true;
        RefreshJoinButtonState();

        try
        {
            SetStatus($"Lobby'ye katılınıyor: {code}...");
            await lobbyCoordinator.JoinAsync(
                lobbyCode: code,
                playerName: playerName,
                onLobbyUpdated: UpdatePlayerListFromLobby
            );
            SetStatus($"Lobiye katıldın. Kod: {code}");
        }
        catch (Exception ex)
        {
            SetStatus($"Katılım başarısız: {ex.Message}");
            Debug.LogException(ex);
        }
        finally
        {
            _isJoining = false;
            RefreshJoinButtonState();
        }
    }

    private void ShowHostCode(string code, bool loading)
    {
        if (hostRoomCodeText == null)
            return;

        hostRoomCodeText.text = loading ? "KOD: ..." : $"KOD: {code}";
        hostRoomCodeText.color = Color.white;
        hostRoomCodeText.fontStyle = FontStyles.Bold;
        if (hostRoomCodeText.fontSize < 36f)
            hostRoomCodeText.fontSize = 42f;
    }

    private void EnsureReadableHostCodeStyle()
    {
        if (hostRoomCodeText == null) return;
        hostRoomCodeText.color = Color.white;
    }

    private void EnsureReadableJoinInputStyle()
    {
        if (joinRoomCodeInput == null) return;

        if (joinRoomCodeInput.textComponent != null)
        {
            joinRoomCodeInput.textComponent.color = Color.white;
            joinRoomCodeInput.textComponent.fontSize = Mathf.Max(joinRoomCodeInput.textComponent.fontSize, 36f);
        }

        if (joinRoomCodeInput.placeholder is TextMeshProUGUI ph)
        {
            ph.color = new Color(1f, 1f, 1f, 0.55f);
            if (string.IsNullOrWhiteSpace(ph.text))
                ph.text = "Oda kodunu girin (6 karakter)";
        }
    }

    private void ResolveAllReferences()
    {
        if (lobbyUi == null)
            lobbyUi = FindAnyObjectByType<LobbyUIManager>();

        if (lobbyCoordinator == null)
            lobbyCoordinator = FindAnyObjectByType<LobbyCoordinator>();

        if (hostRoomCodeText == null)
            hostRoomCodeText = FindUiText("KOD");
        if (joinRoomCodeInput == null || !IsOnMainMenu(joinRoomCodeInput))
            joinRoomCodeInput = FindMainMenuCodeInput() ?? joinRoomCodeInput ?? FindInputField("OdaKodu");
        if (joinPlayerNameInput == null)
            joinPlayerNameInput = FindInputField("Isim");
        if (statusText == null)
            statusText = FindUiText("Durum", "Status");

        EnsureRuntimeStatusText();
    }

    private void EnsureRuntimeStatusText()
    {
        if (statusText != null) return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("Durum_Runtime");
        go.transform.SetParent(canvas.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(900f, 80f);
        rect.anchoredPosition = new Vector2(0f, 40f);

        _runtimeStatusText = go.AddComponent<TextMeshProUGUI>();
        _runtimeStatusText.alignment = TextAlignmentOptions.Center;
        _runtimeStatusText.fontSize = 28f;
        _runtimeStatusText.color = Color.white;
        _runtimeStatusText.text = "Lobby durumu";
        statusText = _runtimeStatusText;
    }

    private void UpdatePlayerListFromLobby(Unity.Services.Lobbies.Models.Lobby lobby)
    {
        if (hostPlayerNameTexts == null || lobby?.Players == null) return;

        ResetHostPlayerList();
        for (int i = 0; i < Mathf.Min(hostPlayerNameTexts.Length, lobby.Players.Count); i++)
        {
            var t = hostPlayerNameTexts[i];
            if (t == null) continue;
            var p = lobby.Players[i];
            if (p?.Data != null && p.Data.TryGetValue(LobbyConstants.PlayerNameKey, out var nameObj))
                t.text = nameObj.Value;
            else
                t.text = "Oyuncu";
        }
    }

    private void ConfigureJoinInputs()
    {
        if (_joinInputsConfigured) return;

        if (joinRoomCodeInput != null)
        {
            joinRoomCodeInput.characterLimit = LobbyConstants.LobbyCodeLength;
            joinRoomCodeInput.contentType = TMP_InputField.ContentType.Alphanumeric;
            joinRoomCodeInput.lineType = TMP_InputField.LineType.SingleLine;
            joinRoomCodeInput.onValueChanged.RemoveListener(OnJoinCodeChanged);
            joinRoomCodeInput.onSubmit.RemoveListener(OnJoinCodeSubmitted);
            joinRoomCodeInput.onValueChanged.AddListener(OnJoinCodeChanged);
            joinRoomCodeInput.onSubmit.AddListener(OnJoinCodeSubmitted);
        }

        if (joinPlayerNameInput != null)
        {
            joinPlayerNameInput.lineType = TMP_InputField.LineType.SingleLine;
            joinPlayerNameInput.characterLimit = 24;
            joinPlayerNameInput.onValueChanged.RemoveListener(OnJoinPlayerNameChanged);
            joinPlayerNameInput.onValueChanged.AddListener(OnJoinPlayerNameChanged);
        }

        _joinInputsConfigured = true;
        RefreshJoinButtonState();
    }

    private void UnbindJoinInputs()
    {
        if (joinRoomCodeInput != null)
        {
            joinRoomCodeInput.onValueChanged.RemoveListener(OnJoinCodeChanged);
            joinRoomCodeInput.onSubmit.RemoveListener(OnJoinCodeSubmitted);
        }
        if (joinPlayerNameInput != null)
            joinPlayerNameInput.onValueChanged.RemoveListener(OnJoinPlayerNameChanged);
        _joinInputsConfigured = false;
    }

    private void ResolveJoinButton()
    {
        if (joinButton != null) return;
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            if (b == null) continue;
            var n = b.gameObject.name ?? string.Empty;
            if (IsMainMenuJoinButtonName(n))
                continue;
            if (n.Equals("Bağlan", StringComparison.OrdinalIgnoreCase) ||
                n.Equals("Baglan", StringComparison.OrdinalIgnoreCase))
            {
                joinButton = b;
                break;
            }
        }
    }

    private void EnsureMainMenuLobbyCodeInput()
    {
        var panel = FindMainMenuPanel();
        if (panel == null) return;

        var existing = FindMainMenuCodeInput();
        if (existing != null)
        {
            joinRoomCodeInput = existing;
            return;
        }

        joinRoomCodeInput = LobbyUiFactory.CreateOdaKoduInput(
            panel.transform,
            anchoredPosition: new Vector2(0f, 360f),
            sizeDelta: new Vector2(520f, 90f));
    }

    private void WireMainMenuJoinButton()
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            if (b == null) continue;
            if (!IsMainMenuJoinButtonName(b.gameObject.name)) continue;

            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(OnMainMenuJoinClicked);
            joinButton = b;
            RefreshJoinButtonState();
            return;
        }
    }

    private void OnMainMenuJoinClicked()
    {
        ResolveAllReferences();
        ConfigureJoinInputs();
        TryJoinLobby();
    }

    private static bool IsMainMenuJoinButtonName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return name.Contains("lobi", StringComparison.OrdinalIgnoreCase) &&
               (name.Contains("bağlan", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("baglan", StringComparison.OrdinalIgnoreCase));
    }

    private static GameObject FindMainMenuPanel()
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t != null && string.Equals(t.gameObject.name, "lobipanel", StringComparison.OrdinalIgnoreCase))
                return t.gameObject;
        }
        return null;
    }

    private static TMP_InputField FindMainMenuCodeInput()
    {
        var panel = FindMainMenuPanel();
        if (panel == null) return null;
        foreach (var input in panel.GetComponentsInChildren<TMP_InputField>(true))
        {
            if (input != null && input.gameObject.name.IndexOf("OdaKodu", StringComparison.OrdinalIgnoreCase) >= 0)
                return input;
        }
        return null;
    }

    private static bool IsOnMainMenu(TMP_InputField input)
    {
        if (input == null) return false;
        var panel = FindMainMenuPanel();
        return panel != null && input.transform.IsChildOf(panel.transform);
    }

    private void RefreshJoinButtonState()
    {
        if (joinButton == null) return;
        var codeOk = joinRoomCodeInput != null &&
                     LobbyConstants.TryValidateLobbyCode(joinRoomCodeInput.text, out _, out _);
        joinButton.interactable = codeOk && !_isJoining && lobbyCoordinator != null;
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

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        Debug.Log($"[LobbyUI] {message}");
    }

    private static TMP_InputField FindInputField(params string[] names)
    {
        foreach (var input in FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include))
        {
            if (input == null) continue;
            var n = (input.gameObject.name ?? string.Empty).Trim();
            foreach (var c in names)
                if (string.Equals(n, c, StringComparison.OrdinalIgnoreCase)) return input;
        }
        return null;
    }

    private static TextMeshProUGUI FindUiText(params string[] names)
    {
        foreach (var t in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
        {
            if (t == null) continue;
            var n = t.gameObject.name ?? string.Empty;
            foreach (var c in names)
                if (n.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0) return t;
        }
        return null;
    }

    private void EnsureArraySize()
    {
        if (hostPlayerNameTexts == null)
            hostPlayerNameTexts = new TextMeshProUGUI[4];
        else if (hostPlayerNameTexts.Length != 4)
            Array.Resize(ref hostPlayerNameTexts, 4);
    }
}
