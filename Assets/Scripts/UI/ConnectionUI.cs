using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class ConnectionUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject connectionPanel;
    [SerializeField] private TMP_InputField ipAddressInput;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button disconnectButton;

    private NetworkManager networkManager;

    void Start()
    {
        networkManager = NetworkManager.singleton;

        if (ipAddressInput != null)
        {
            ipAddressInput.text = "localhost";
        }

        if (hostButton != null)
            hostButton.onClick.AddListener(OnHostClick);
        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClick);
        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(OnDisconnectClick);

        UpdateUI(false);
    }

    public void OnHostClick()
    {
        if (networkManager == null) return;

        Debug.Log("[UI] Запуск хоста...");
        networkManager.StartHost();
        UpdateUI(true);
    }

    public void OnConnectClick()
    {
        if (networkManager == null) return;

        string address = ipAddressInput != null ? ipAddressInput.text : "localhost";
        if (string.IsNullOrWhiteSpace(address))
            address = "localhost";

        networkManager.networkAddress = address;

        Debug.Log($"[UI] Подключение к {address}...");
        networkManager.StartClient();
        UpdateUI(true);
    }

    public void OnDisconnectClick()
    {
        Debug.Log("[UI] Нажата кнопка Disconnect.");

        if (NetworkServer.active && NetworkClient.isConnected)
        {
            networkManager.StopHost();
            Debug.Log("[UI] Хост остановлен.");
        }
        else if (NetworkClient.isConnected)
        {
            networkManager.StopClient();
            Debug.Log("[UI] Клиент отключен.");
        }
        else if (NetworkServer.active)
        {
            networkManager.StopServer();
            Debug.Log("[UI] Сервер остановлен.");
        }

        UpdateUI(false);
    }

    private void UpdateUI(bool isConnected)
    {
        if (connectionPanel != null)
            connectionPanel.SetActive(!isConnected);

        if (disconnectButton != null)
            disconnectButton.gameObject.SetActive(isConnected);

        if (!isConnected)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Update()
    {
        if (NetworkClient.isConnected && connectionPanel != null && connectionPanel.activeSelf)
        {
            UpdateUI(true);
        }
        else if (!NetworkClient.isConnected && !NetworkServer.active
                 && disconnectButton != null && disconnectButton.gameObject.activeSelf)
        {
            UpdateUI(false);
        }
    }
}
