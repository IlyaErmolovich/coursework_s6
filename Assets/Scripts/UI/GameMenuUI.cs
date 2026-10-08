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
    [SerializeField] public TextMeshProUGUI teamScoreText;
     [SerializeField] public TextMeshProUGUI artifactCountText;
    
    [Header("Interaction Hint")]
    [SerializeField] private TextMeshProUGUI interactionText;

    [Header("Stamina UI")]
    [SerializeField] private Slider staminaSlider;

    [Header("Stun UI")]
    [SerializeField] private GameObject stunOverlay;

    [Header("Hacking Progress")]
    [SerializeField] private Slider hackingProgressSlider;
    [SerializeField] private GameObject hackingProgressPanel;

    [Header("Victory Conditions")]
    [SerializeField] private TextMeshProUGUI targetMoneyText;   

    private bool _isOpen;
    private PlayerInventory _localInventory;
    private PlayerInteraction _localInteraction;
    private PlayerController _localController;
    private PlayerLobbyData _localLobbyData;
    private int _displayedMoney = int.MinValue;
    private int _displayedSlots = int.MinValue;
    private int _displayedMaxSlots = int.MinValue;
    private int _displayedTarget = int.MinValue;
    private string _displayedInteractionText;

    void Start()
    {
        if (escapePanel != null) escapePanel.SetActive(false);
        if (interactionText != null) interactionText.gameObject.SetActive(false);

        if (hackingProgressPanel != null) hackingProgressPanel.SetActive(false);
            JailDoor.OnHackProgressChanged += OnHackProgress;
        
        _isOpen = false;
        ApplyCursorState();
    }

    private void Update()
    {
        if (GameManager.singleton != null && GameManager.singleton.IsMatchEnded) return;

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
            bool stunned = _localController.IsStunned;
            if (stunOverlay.activeSelf != stunned) stunOverlay.SetActive(stunned);
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
                _localLobbyData = NetworkClient.localPlayer.GetComponent<PlayerLobbyData>();
            }
            return;
        }

        
        bool isThief = _localLobbyData != null && _localLobbyData.currentTeam == PlayerTeam.Thieves;

        
        if (_localInteraction != null && interactionText != null)
        {
            
            var interactable = _localInteraction.GetCurrentInteractable;
            
            
            if (interactable != null && (isThief || interactable.CanGuardsInteract))
            {
                if (!interactionText.gameObject.activeSelf) interactionText.gameObject.SetActive(true);
                string hint = interactable.GetInteractionText(_localInventory);
                if (_displayedInteractionText != hint)
                {
                    interactionText.text = hint;
                    _displayedInteractionText = hint;
                }
            }
            else
            {
                if (interactionText.gameObject.activeSelf) interactionText.gameObject.SetActive(false);
            }
        }

        
        if (staminaSlider != null)
        {
            if (staminaSlider.gameObject.activeSelf != isThief) staminaSlider.gameObject.SetActive(isThief);
            if (isThief && _localController != null)
            {
                staminaSlider.value = _localController.StaminaProgress;
            }
        }

        
        if (moneyText != null && moneyText.gameObject.activeSelf != isThief) moneyText.gameObject.SetActive(isThief);
        if (slotsText != null && slotsText.gameObject.activeSelf != isThief) slotsText.gameObject.SetActive(isThief);

        if (isThief)
        {
            int money = _localInventory.TotalMoney;
            if (moneyText != null && money != _displayedMoney)
            {
                moneyText.text = $"$: {money}";
                _displayedMoney = money;
            }

            int slots = _localInventory.OccupiedSlots;
            int maxSlots = _localInventory.MaxSlots;
            if (slotsText != null && (slots != _displayedSlots || maxSlots != _displayedMaxSlots))
            {
                slotsText.text = $"Слоты: {slots}/{maxSlots}";
                _displayedSlots = slots;
                _displayedMaxSlots = maxSlots;
            }

            if (targetMoneyText != null && GameManager.singleton != null)
            {
                int target = GameManager.singleton.targetDepositScore;
                if (target != _displayedTarget)
                {
                    targetMoneyText.text = $"Цель: ${target}";
                    _displayedTarget = target;
                }
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
        if (GameManager.singleton != null && GameManager.singleton.IsMatchEnded) return;

        _isOpen = !_isOpen;
        if (escapePanel != null) escapePanel.SetActive(_isOpen);
        
        if (_isOpen && interactionText != null) interactionText.gameObject.SetActive(false);
        
        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        if (GameManager.singleton != null && GameManager.singleton.IsMatchEnded) return;

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

    void OnDestroy()
    {
        JailDoor.OnHackProgressChanged -= OnHackProgress;
    }

    private void OnHackProgress(float progress)
    {
        if (hackingProgressPanel == null) return;
        if (progress < 0f || progress >= 1f)
        {
            hackingProgressPanel.SetActive(false);
            return;
        }
        hackingProgressPanel.SetActive(true);
        if (hackingProgressSlider != null)
            hackingProgressSlider.value = progress;
    }

    private void OnEnable()
    {
        TeamScoreManager.OnDepositUpdated += UpdateTeamScore;
        TeamScoreManager.OnArtifactDepositUpdated += UpdateArtifactCount;
    }

    private void OnDisable()
    {
        TeamScoreManager.OnDepositUpdated -= UpdateTeamScore;
    }

    private void UpdateArtifactCount(int count)
    {
        if (artifactCountText != null)
            artifactCountText.text = $"Артефактов сдано: {count}";
    }

    private void UpdateTeamScore(int score)
    {
        if (teamScoreText != null)
            teamScoreText.text = $"Сдано: ${score}";
    }
}
