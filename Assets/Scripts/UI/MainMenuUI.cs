using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;      // Кнопки: Играть, Выход
    [SerializeField] private GameObject connectPanel;   // Поле IP, кнопки: Хост, Клиент, Назад
    [SerializeField] private TMP_InputField nameInputField;

    [Header("Input Elements")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    void Start()
    {
        // Показываем только главную панель
        mainPanel.SetActive(true);
        connectPanel.SetActive(false);

        // Устанавливаем дефолтный адрес в поле ввода, чтобы не было ошибки
        if (string.IsNullOrEmpty(ipInputField.text))
            ipInputField.text = "localhost";

        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Вызывается кнопкой "Играть"
    public void OpenConnectPanel()
    {
        mainPanel.SetActive(false);
        connectPanel.SetActive(true);
    }

    // Вызывается кнопкой "Назад"
    public void OpenMainPanel()
    {
        connectPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void StartHost()
    {
        if (NetworkServer.active || NetworkClient.active) return;

        SaveName(); // ДОБАВЬ ЭТУ СТРОКУ
        NetworkManager.singleton.StartHost();
    }

    public void StartClient()
    {
        if (NetworkClient.active) return;

        SaveName();

        // Если поле пустое — подставляем localhost принудительно
        string address = string.IsNullOrEmpty(ipInputField.text) ? "localhost" : ipInputField.text;
        
        NetworkManager.singleton.networkAddress = address;
        NetworkManager.singleton.StartClient();
    }

    private void SaveName()
    {
        string n = string.IsNullOrEmpty(nameInputField.text) ? "Player" : nameInputField.text;
        PlayerPrefs.SetString("PlayerName", n);
        PlayerPrefs.Save(); // Гарантирует запись данных на диск
    }

    public void ExitGame() => Application.Quit();
}