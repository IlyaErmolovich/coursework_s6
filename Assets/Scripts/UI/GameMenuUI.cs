using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class GameMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject escapePanel;
    private bool _isOpen;

    void Start()
    {
        if (escapePanel != null) escapePanel.SetActive(false);
        _isOpen = false;
        
        // При старте гарантируем игровое состояние курсора
        ApplyCursorState();
    }

    void Update()
    {
        // Проверяем нажатие Escape напрямую в Update
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        _isOpen = !_isOpen;
        if (escapePanel != null) escapePanel.SetActive(_isOpen);
        
        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        // 1. Управление мышкой
        Cursor.lockState = _isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = _isOpen;

        // 2. Блокировка управления персонажем (локального игрока)
        if (NetworkClient.localPlayer != null)
        {
            var controller = NetworkClient.localPlayer.GetComponent<PlayerController>();
            if (controller != null)
            {
                // Если меню открыто — выключаем контроллер, если закрыто — включаем
                controller.enabled = !_isOpen;
            }
        }
    }

    public void Disconnect()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else
            NetworkManager.singleton.StopClient();
    }
}