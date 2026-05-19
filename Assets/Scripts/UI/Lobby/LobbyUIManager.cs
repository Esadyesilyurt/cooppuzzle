using System;
using UnityEngine;

public sealed class LobbyUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject joinPanel;

    private void Awake()
    {
        ResolvePanelReferences();
        ShowMainMenu();
    }

    public void ShowMainMenu() => ShowLobbyPanelOnly();

    /// <summary>Host/Join kapat; yalnızca lobipanel (ana menü) açık kalsın.</summary>
    public void ShowLobbyPanelOnly()
    {
        ResolvePanelReferences();
        SetActive(hostPanel, false);
        SetActive(joinPanel, false);

        if (mainMenuPanel != null)
        {
            SetActiveWithParents(mainMenuPanel, true);
            BringToFront(mainMenuPanel);
        }
        else
        {
            Debug.LogWarning("[LobbyUI] lobipanel bulunamadı.");
        }
    }

    public void OpenHostPanel()
    {
        ResolvePanelReferences();

        SetActive(mainMenuPanel, false);
        SetActive(joinPanel, false);

        if (hostPanel != null)
        {
            SetActiveWithParents(hostPanel, true);
            BringToFront(hostPanel);

            var hostUi = hostPanel.GetComponent<HostPanelController>();
            hostUi?.WireIfNeeded();
        }
        else
        {
            Debug.LogError("[LobbyUI] HostPanel bulunamadı. Sahnedeki 'HostPanel' objesini LobbyUIManager'a bağla.");
        }
    }

    public void OpenJoinPanel()
    {
        ResolvePanelReferences();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        if (hostPanel != null)
            hostPanel.SetActive(false);

        if (joinPanel == null)
            joinPanel = FindPanelRoot("JoinPanel");

        if (joinPanel != null)
        {
            SetActiveWithParents(joinPanel, true);
            BringToFront(joinPanel);
        }
        else
        {
            Debug.LogError("[LobbyUI] JoinPanel bulunamadı. LobbyUIManager.joinPanel alanını bağla.");
        }

        var lobby = FindAnyObjectByType<GameLobbyController>();
        lobby?.PrepareJoinPanel();
    }

    private void ResolvePanelReferences()
    {
        if (mainMenuPanel == null)
            mainMenuPanel = FindPanelRoot("lobipanel");
        if (hostPanel == null)
            hostPanel = FindPanelRoot("HostPanel");
        if (joinPanel == null)
            joinPanel = FindPanelRoot("JoinPanel");
    }

    private static GameObject FindPanelRoot(string panelName)
    {
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && string.Equals(t.gameObject.name, panelName, StringComparison.OrdinalIgnoreCase))
                return t.gameObject;
        }

        return null;
    }

    private static void BringToFront(GameObject panel)
    {
        if (panel == null) return;
        panel.transform.SetAsLastSibling();
    }

    private static void SetActiveWithParents(GameObject go, bool isActive)
    {
        if (go == null) return;

        if (isActive)
        {
            var t = go.transform.parent;
            while (t != null)
            {
                if (!t.gameObject.activeSelf)
                    t.gameObject.SetActive(true);
                t = t.parent;
            }
        }

        go.SetActive(isActive);
    }

    private static void SetActive(GameObject go, bool isActive)
    {
        if (go == null) return;
        if (go.activeSelf == isActive) return;
        go.SetActive(isActive);
    }
}

