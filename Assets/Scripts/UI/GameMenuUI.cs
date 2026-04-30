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
    [SerializeField] private TextMeshProUGUI interactionText;

    [Header("Stamina UI")]
    [SerializeField] private Slider staminaSlider;

    [Header("Stun UI")]
    [SerializeField] private GameObject stunOverlay;

    private bool _isOpen;
    private PlayerInventory _localInventory;
    private PlayerInteraction _localInteraction;
    private PlayerController _localController;

    void Start()
    {
        if (escapePanel != null) escapePanel.SetActive(false);
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
            UpdateStunUI();
        }
    }

    private void UpdateStunUI()
    {
        if (_localController != null && stunOverlay != null)
        {
            stunOverlay.SetActive(_localController.IsStunned);
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

        if (staminaSlider != null && _localController != null)
            staminaSlider.value = _localController.StaminaProgress;

        if (moneyText != null) moneyText.text = $"$: {_localInventory.TotalMoney}";
        if (slotsText != null) slotsText.text = $"Slots: {_localInventory.OccupiedSlots}/{_localInventory.MaxSlots}";

        if (_localInteraction != null && interactionText != null)
        {
            var interactable = _localInteraction.GetCurrentInteractable;
            
            if (interactable != null)
            {
                interactionText.gameObject.SetActive(true);
                interactionText.text = interactable.GetInteractionText(_localInventory);
            }
            else
            {
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