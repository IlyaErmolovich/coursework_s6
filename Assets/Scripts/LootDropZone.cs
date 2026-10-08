using UnityEngine;
using Mirror;

public class LootDropZone : NetworkBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string zoneName = "Фургон";
    [SerializeField] private float interactionRadius = 3f;
    private string _cachedApproachZoneName;
    private string _cachedApproachText;
    private int _cachedMoney = int.MinValue;
    private string _cachedDepositText;

    public bool CanGuardsInteract => false;

    private float GetDistanceToPlayer(PlayerInventory inventory)
    {
        if (inventory == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, inventory.transform.position);
    }

    private bool IsThief(PlayerInventory inventory)
    {
        var lobby = inventory.GetComponent<PlayerLobbyData>();
        return lobby != null && lobby.currentTeam == PlayerTeam.Thieves;
    }

    public string GetInteractionText(PlayerInventory inventory)
    {
        if (!IsThief(inventory)) return "";

        float dist = GetDistanceToPlayer(inventory);
        if (dist > interactionRadius)
        {
            if (_cachedApproachText == null || _cachedApproachZoneName != zoneName)
            {
                _cachedApproachZoneName = zoneName;
                _cachedApproachText = $"Подойдите к {zoneName}";
            }
            return _cachedApproachText;
        }
        if (inventory.TotalMoney <= 0) return "Нет денег для сдачи";
        int money = inventory.TotalMoney;
        if (_cachedDepositText == null || _cachedMoney != money)
        {
            _cachedMoney = money;
            _cachedDepositText = $"Нажмите E, чтобы сдать награбленное (${money})";
        }
        return _cachedDepositText;
    }

    public bool CanInteract(PlayerInventory inventory)
    {
        if (!IsThief(inventory)) return false;
        return GetDistanceToPlayer(inventory) <= interactionRadius && inventory.TotalMoney > 0;
    }

    public void Interact(PlayerInventory inventory)
    {
        inventory.CmdDepositMoney(inventory.TotalMoney);
    }

    private void OnDrawGizmosSelected() 
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
