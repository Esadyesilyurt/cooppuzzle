using UnityEngine;

public class LobbyUIManager : MonoBehaviour
{
    [Header("Paneller")]
    public GameObject mainMenuPanel; // Senin "lobipanel" objen
    public GameObject hostPanel;     // Lobi kuranýn ekraný
    public GameObject joinPanel;     // Baðlananýn ekraný

    void Start()
    {
        // Oyun baþladýðýnda sadece ana menü açýk olsun, diðerleri kapalý olsun
        ShowMainMenu();
    }

    public void OpenHostPanel()
    {
        mainMenuPanel.SetActive(false);
        hostPanel.SetActive(true);
        joinPanel.SetActive(false);
    }

    public void OpenJoinPanel()
    {
        mainMenuPanel.SetActive(false);
        hostPanel.SetActive(false);
        joinPanel.SetActive(true);
    }

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        hostPanel.SetActive(false);
        joinPanel.SetActive(false);
    }
}