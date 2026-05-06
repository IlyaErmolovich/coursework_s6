using UnityEngine;
using Mirror;

public class LootDropZone : NetworkBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string zoneName = "Фургон";
    [SerializeField] private float interactionRadius = 3f;

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
        if (dist > interactionRadius) return $"Подойдите к {zoneName}";
        if (inventory.TotalMoney <= 0) return "Нет денег для сдачи";
        return $"Нажмите E, чтобы сдать награбленное (${inventory.TotalMoney})";
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