using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class GameMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject escapePanel;
    
    [Header("Inventory HUD")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI slotsText;
    
    [Header("Interaction Hint")]
    // Оставляем только текст. Будем включать/выключать сам объект текста.
    [SerializeField] private TextMeshProUGUI interactionText;

    [Header("Stamina UI")]
    [SerializeField] private Slider staminaSlider;

    private bool _isOpen;
    private PlayerInventory _localInventory;
    private PlayerInteraction _localInteraction;
    private PlayerController _localController;

    void Start()
    {
        if (escapePanel != null) escapePanel.SetActive(false);
        // При старте скрываем текст подсказки
        if (interactionText != null) interactionText.gameObject.SetActive(false);
        
        _isOpen = false;
        ApplyCursorState();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }

        if (!_isOpen)
        {
            UpdateHUD();
        }
    }

    private void UpdateHUD()
    {
        if (_localInventory == null)
        {
            if (NetworkClient.localPlayer != null)
            {
                _localInventory = NetworkClient.localPlayer.GetComponent<PlayerInventory>();
                _localInteraction = NetworkClient.localPlayer.GetComponentInChildren<PlayerInteraction>();
                _localController = NetworkClient.localPlayer.GetComponent<PlayerController>();
            }
            return;
        }

        // 1. Стамина
        if (staminaSlider != null && _localController != null)
            staminaSlider.value = _localController.StaminaProgress;

        // 2. Инвентарь
        if (moneyText != null) moneyText.text = $"$: {_localInventory.TotalMoney}";
        if (slotsText != null) slotsText.text = $"Slots: {_localInventory.OccupiedSlots}/{_localInventory.MaxSlots}";

        // 3. Подсказка взаимодействия
        if (_localInteraction != null && interactionText != null)
        {
            var interactable = _localInteraction.GetCurrentInteractable;
            
            if (interactable != null)
            {
                // Включаем объект текста и меняем его содержимое
                interactionText.gameObject.SetActive(true);
                interactionText.text = interactable.GetInteractionText(_localInventory);
            }
            else
            {
                // Если не смотрим на предмет — выключаем объект текста
                interactionText.gameObject.SetActive(false);
            }
        }
    }

    public void Resume()
    {
        if (_isOpen)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        _isOpen = !_isOpen;
        if (escapePanel != null) escapePanel.SetActive(_isOpen);
        
        // Если открыли меню, принудительно скрываем подсказку
        if (_isOpen && interactionText != null) interactionText.gameObject.SetActive(false);
        
        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        Cursor.lockState = _isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = _isOpen;

        if (_localController != null)
            _localController.enabled = !_isOpen;
    }

    public void Disconnect()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();
    }
}