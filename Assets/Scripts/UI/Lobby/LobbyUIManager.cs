using UnityEngine;

public sealed class LobbyUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject joinPanel;

    private void Awake()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        SetActive(mainMenuPanel, true);
        SetActive(hostPanel, false);
        SetActive(joinPanel, false);
    }

    public void OpenHostPanel()
    {
        SetActive(mainMenuPanel, false);
        SetActive(joinPanel, false);
        SetActive(hostPanel, true);
        BringToFront(hostPanel);

    }

    public void OpenJoinPanel()
    {
        SetActive(mainMenuPanel, false);
        SetActive(hostPanel, false);
        SetActive(joinPanel, true);
        BringToFront(joinPanel);

        var lobby = FindAnyObjectByType<GameLobbyController>();
        lobby?.PrepareJoinPanel();
    }

    private static void BringToFront(GameObject panel)
    {
        if (panel == null) return;
        panel.transform.SetAsLastSibling();
    }

    private static void SetActive(GameObject go, bool isActive)
    {
        if (go == null) return;
        if (go.activeSelf == isActive) return;
        go.SetActive(isActive);
    }
}

