using UnityEngine;
using Mirror;

public class PickupItem : NetworkBehaviour, IInteractable
{
    [Header("Item Settings")]
    [SerializeField] private string itemName = "Предмет";
    [SerializeField] private int value = 100;

    public string GetInteractionText(PlayerInventory inventory)
    {
        if (!inventory.HasFreeSlot()) 
            return "Нет свободного места";
            
        return $"Нажмите E, чтобы взять {itemName} (Стоимость: {value})";
    }

    // Теперь ВСЕ предметы проверяют наличие свободного слота
    public bool CanInteract(PlayerInventory inventory) => inventory.HasFreeSlot();

    public void Interact(PlayerInventory inventory)
    {
        inventory.CmdAddItem(value);

        // Ищем аудиоменеджер на игроке, который инициировал взаимодействие
        PlayerAudioManager playerAudio = inventory.GetComponent<PlayerAudioManager>();
        if (playerAudio != null)
        {
            playerAudio.PlayPickupLocal();
        }

        CmdDestroySelf();
    }

    [Command(requiresAuthority = false)]
    private void CmdDestroySelf() => NetworkServer.Destroy(gameObject);
}