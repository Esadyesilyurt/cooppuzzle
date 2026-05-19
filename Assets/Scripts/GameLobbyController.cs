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
    [SerializeField] private HostPanelController hostPanelController;

    public string WaitingLabel => waitingText;

    private bool _joinInputsConfigured;
    private bool _isJoining;
    private string _lastHostCodeLogged;
    private TextMeshProUGUI _runtimeStatusText;

    private void HandleLobbyUpdated(Unity.Services.Lobbies.Models.Lobby lobby)
    {
        if (lobby == null)
            return;

        ShowHostCode(lobby.LobbyCode, loading: false);
        RefreshHostPanel(lobby);
        lobbyCoordinator?.TryFollowGameStart(lobby);
        lobbyCoordinator?.TryFollowGameEnd(lobby);
        lobbyCoordinator?.TryFollowLobbyClosed(lobby);
    }

    private void Awake()
    {
        EnsureArraySize();
        HideMainMenuLobbyCodeField();
        ResolveAllReferences();
        ConfigureJoinInputs();
        WireJoinPanelButtons();
        WirePanelBackButtons();
        WireMainMenuJoinButton();
        ResolveHostPanelController();
        EnsureReadableHostCodeStyle();
        EnsureReadableJoinInputStyle();

        if (lobbyCoordinator != null)
        {
            lobbyCoordinator.StatusChanged += SetStatus;
            lobbyCoordinator.LobbySessionEnded += OnLobbySessionEnded;
        }
    }

    private void OnDestroy()
    {
        if (lobbyCoordinator != null)
        {
            lobbyCoordinator.StatusChanged -= SetStatus;
            lobbyCoordinator.LobbySessionEnded -= OnLobbySessionEnded;
        }

        UnbindJoinInputs();
    }

    public void HostLobby()
    {
        PrepareHostPanel();

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
        ResolveHostPanelController();

        lobbyUi?.OpenHostPanel();
        EnsureHostPanelVisible();

        hostPanelController?.SetHostView();
        hostPanelController?.WireIfNeeded();
        WirePanelBackButtons();
        hostPanelController?.RefreshFromLobby(null, waitingText);
        ShowHostCode("------", loading: true);
        SetStatus("Lobi kuruluyor, kod birazdan görünecek...");
    }

    public void OpenJoinMenu()
    {
        ResolveAllReferences();
        lobbyUi?.OpenJoinPanel();
        EnsureJoinPanelVisible();
    }

    private void EnsureJoinPanelVisible()
    {
        var join = FindJoinPanelObject();
        var menu = FindMainMenuPanel();
        var host = FindHostPanelObject();

        if (menu != null && menu.activeSelf)
            menu.SetActive(false);

        if (host != null && host.activeSelf)
            host.SetActive(false);

        if (join == null)
        {
            SetStatus("JoinPanel bulunamadı. Sahneye JoinPanel ekle.");
            return;
        }

        var t = join.transform.parent;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            t = t.parent;
        }

        if (!join.activeSelf)
            join.SetActive(true);

        join.transform.SetAsLastSibling();
    }

    private void EnsureHostPanelVisible()
    {
        var host = FindHostPanelObject();
        var menu = FindMainMenuPanel();

        if (menu != null && menu.activeSelf)
            menu.SetActive(false);

        if (host == null)
            return;

        var t = host.transform.parent;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            t = t.parent;
        }

        if (!host.activeSelf)
            host.SetActive(true);

        host.transform.SetAsLastSibling();
    }

    public void SetStatusPublic(string message) => SetStatus(message);

    /// <summary>HostPanel / JoinPanel → lobipanel. Aktif lobideyse önce lobiyi kapatır.</summary>
    public void ReturnToLobbyPanel()
    {
        ResolveAllReferences();

        if (lobbyCoordinator != null && HasActiveLobbySession())
        {
            _ = ReturnToLobbyPanelAfterLeaveAsync();
            return;
        }

        ShowLobbyPanelOnlyUi();
    }

    private bool HasActiveLobbySession()
    {
        if (lobbyCoordinator != null && lobbyCoordinator.HasActiveLobbySession())
            return true;

        if (lobbyCoordinator?.LobbyService?.CurrentLobby != null)
            return true;

        var bootstrap = lobbyCoordinator != null
            ? FindAnyObjectByType<CoopPuzzle.Core.Bootstrap.NetworkBootstrap>()
            : null;

        return bootstrap != null && bootstrap.IsConnected;
    }

    private async System.Threading.Tasks.Task ReturnToLobbyPanelAfterLeaveAsync()
    {
        try
        {
            await lobbyCoordinator.LeaveOrCloseLobbyAsync();
        }
        catch (Exception ex)
        {
            SetStatus($"Lobi kapatılamadı: {ex.Message}");
            Debug.LogException(ex);
            OnLobbySessionEnded();
        }
    }

    private void OnLobbySessionEnded()
    {
        ShowLobbyPanelOnlyUi();
        ShowHostCode("------", loading: true);
        ResetHostPlayerList();
        hostPanelController?.RefreshFromLobby(null, waitingText);
    }

    private void ShowLobbyPanelOnlyUi()
    {
        if (lobbyUi != null)
        {
            lobbyUi.ShowLobbyPanelOnly();
            return;
        }

        var host = FindHostPanelObject();
        var join = FindJoinPanelObject();
        var menu = FindMainMenuPanel();

        if (host != null)
            host.SetActive(false);
        if (join != null)
            join.SetActive(false);

        if (menu == null)
        {
            SetStatus("lobipanel bulunamadı.");
            return;
        }

        var t = menu.transform.parent;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            t = t.parent;
        }

        menu.SetActive(true);
        menu.transform.SetAsLastSibling();
    }

    public void StartGame()
    {
        if (lobbyCoordinator == null)
        {
            SetStatus("LobbyCoordinator bulunamadı.");
            return;
        }

        if (!lobbyCoordinator.IsLocalPlayerHost())
        {
            SetStatus("Oyunu yalnızca host başlatabilir.");
            return;
        }

        _ = RunStartGameAsync();
    }

    public void PrepareJoinPanel()
    {
        ResolveAllReferences();
        UnbindJoinInputs();
        ConfigureJoinInputs();
        WireJoinPanelButtons();
        WirePanelBackButtons();

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
        SetStatus($"Lobi kodunu ve ismini gir, sonra Bağlan'a bas.");
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
                    hostPanelController?.SetHostView();
                    HandleLobbyUpdated(lobby);
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
                onLobbyUpdated: HandleLobbyUpdated
            );

            if (lobbyCoordinator.IsLocalPlayerHost())
            {
                SetStatus("Bu örnek host hesabı kullanıyor. Client için ikinci Unity penceresi veya build aç.");
                return;
            }

            if (lobbyCoordinator.LobbyService == null || !lobbyCoordinator.LobbyService.IsLocalPlayerInLobby())
            {
                SetStatus("Lobby'ye eklenemedin. Kodu ve bağlantıyı kontrol et.");
                return;
            }

            OpenLobbyRoomAsClient(code);
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
        joinRoomCodeInput = FindJoinPanelInput("OdaKodu") ?? joinRoomCodeInput;
        joinPlayerNameInput = FindJoinPanelInput("Isim") ?? joinPlayerNameInput;
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
        RefreshHostPanel(lobby);
    }

    private void OpenLobbyRoomAsClient(string code)
    {
        ResolveHostPanelController();
        lobbyUi?.OpenHostPanel();
        EnsureHostPanelVisible();

        hostPanelController?.SetClientView(true);
        hostPanelController?.WireIfNeeded();
        hostPanelController?.RefreshFromLobby(lobbyCoordinator?.LobbyService?.CurrentLobby, waitingText);
        ShowHostCode(code, loading: false);
    }

    private void RefreshHostPanel(Unity.Services.Lobbies.Models.Lobby lobby)
    {
        ResolveHostPanelController();
        if (lobbyCoordinator != null && !lobbyCoordinator.IsLocalPlayerHost())
            hostPanelController?.SetClientView(true);
        else
            hostPanelController?.SetHostView();

        hostPanelController?.RefreshFromLobby(lobby, waitingText);

        if (hostPlayerNameTexts == null || lobby?.Players == null) return;

        for (int slot = 0; slot < hostPlayerNameTexts.Length; slot++)
        {
            var t = hostPlayerNameTexts[slot];
            if (t == null) continue;
            t.text = waitingText;
        }

        foreach (var p in lobby.Players)
        {
            var slot = LobbySlotLayout.GetSlotIndexForPlayer(p);
            if (slot < 0 || slot >= hostPlayerNameTexts.Length) continue;
            var t = hostPlayerNameTexts[slot];
            if (t == null) continue;
            if (p.Data != null && p.Data.TryGetValue(LobbyConstants.PlayerNameKey, out var nameObj)
                && !string.IsNullOrWhiteSpace(nameObj.Value))
                t.text = nameObj.Value;
            else
                t.text = "Oyuncu";
        }
    }

    private void ResolveHostPanelController()
    {
        if (hostPanelController != null) return;
        hostPanelController = FindAnyObjectByType<HostPanelController>();

        if (hostPanelController != null) return;

        var hostPanelGo = FindHostPanelObject();
        if (hostPanelGo == null) return;

        hostPanelController = hostPanelGo.GetComponent<HostPanelController>();
        if (hostPanelController == null)
            hostPanelController = hostPanelGo.AddComponent<HostPanelController>();
    }

    private static GameObject FindHostPanelObject()
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t != null && string.Equals(t.gameObject.name, "HostPanel", StringComparison.OrdinalIgnoreCase))
                return t.gameObject;
        }

        return null;
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

    private void HideMainMenuLobbyCodeField()
    {
        var panel = FindMainMenuPanel();
        if (panel == null) return;

        foreach (var input in panel.GetComponentsInChildren<TMP_InputField>(true))
        {
            if (input == null) continue;
            if (input.gameObject.name.IndexOf("OdaKodu", StringComparison.OrdinalIgnoreCase) >= 0)
                input.gameObject.SetActive(false);
        }
    }

    private void WireMainMenuJoinButton()
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            if (b == null) continue;
            if (!IsMainMenuJoinButtonName(b.gameObject.name)) continue;

            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(OpenJoinMenu);
            b.interactable = true;
            return;
        }
    }

    private void WireJoinPanelButtons()
    {
        joinButton = null;
        var joinPanel = FindJoinPanelObject();
        if (joinPanel == null) return;

        foreach (var b in joinPanel.GetComponentsInChildren<Button>(true))
        {
            if (b == null) continue;
            var n = b.gameObject.name ?? string.Empty;

            if (n.Equals("Bağlan", StringComparison.OrdinalIgnoreCase) ||
                n.Equals("Baglan", StringComparison.OrdinalIgnoreCase))
            {
                joinButton = b;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(JoinLobby);
                continue;
            }

            if (IsBackButtonName(n))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(ReturnToLobbyPanel);
            }
        }

        RefreshJoinButtonState();
    }

    private void WirePanelBackButtons()
    {
        WireBackButtonsInPanel(FindHostPanelObject());
        WireBackButtonsInPanel(FindJoinPanelObject());
    }

    private void WireBackButtonsInPanel(GameObject panel)
    {
        if (panel == null)
            return;

        foreach (var b in panel.GetComponentsInChildren<Button>(true))
        {
            if (b == null || !IsBackButtonName(b.gameObject.name))
                continue;

            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(ReturnToLobbyPanel);
        }
    }

    private static bool IsBackButtonName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        if (name.Contains("Geri", StringComparison.OrdinalIgnoreCase))
            return true;

        if (name.Equals("Back", StringComparison.OrdinalIgnoreCase))
            return true;

        return name.Contains("Back", StringComparison.OrdinalIgnoreCase)
               && !name.Contains("Background", StringComparison.OrdinalIgnoreCase);
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

    private static TMP_InputField FindJoinPanelInput(string fieldName)
    {
        var panel = FindJoinPanelObject();
        if (panel == null) return null;

        foreach (var input in panel.GetComponentsInChildren<TMP_InputField>(true))
        {
            if (input != null && string.Equals(input.gameObject.name, fieldName, StringComparison.OrdinalIgnoreCase))
                return input;
        }

        return null;
    }

    private static GameObject FindJoinPanelObject()
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t != null && string.Equals(t.gameObject.name, "JoinPanel", StringComparison.OrdinalIgnoreCase))
                return t.gameObject;
        }

        return null;
    }

    private void RefreshJoinButtonState()
    {
        if (joinButton == null) return;
        var codeOk = joinRoomCodeInput != null &&
                     LobbyConstants.TryValidateLobbyCode(joinRoomCodeInput.text, out _, out _);
        var nameOk = joinPlayerNameInput == null ||
                     !string.IsNullOrWhiteSpace(joinPlayerNameInput.text);
        joinButton.interactable = codeOk && nameOk && !_isJoining && lobbyCoordinator != null;
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
