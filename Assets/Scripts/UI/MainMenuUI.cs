using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;      
    [SerializeField] private GameObject connectPanel;   
    [SerializeField] private TMP_InputField nameInputField;

    [Header("Input Elements")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    void Start()
    {
        mainPanel.SetActive(true);
        connectPanel.SetActive(false);

        if (string.IsNullOrEmpty(ipInputField.text))
            ipInputField.text = "localhost";

        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenConnectPanel()
    {
        mainPanel.SetActive(false);
        connectPanel.SetActive(true);
    }

    public void OpenMainPanel()
    {
        connectPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void StartHost()
    {
        if (NetworkServer.active || NetworkClient.active) return;

        SaveName();
        NetworkManager.singleton.StartHost();
    }

    public void StartClient()
    {
        if (NetworkClient.active) return;

        SaveName();

        string address = string.IsNullOrEmpty(ipInputField.text) ? "localhost" : ipInputField.text;
        
        NetworkManager.singleton.networkAddress = address;
        NetworkManager.singleton.StartClient();
    }

    private void SaveName()
    {
        string n = string.IsNullOrEmpty(nameInputField.text) ? "Player" : nameInputField.text;
        PlayerPrefs.SetString("PlayerName", n);
        PlayerPrefs.Save();
    }

    public void ExitGame() => Application.Quit();
}