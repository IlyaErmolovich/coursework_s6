using UnityEngine;
using Mirror;

public class PickupItem : NetworkBehaviour, IInteractable
{
    [Header("Item Settings")]
    [SerializeField] private string itemName = "Предмет";
    [SerializeField, SyncVar] private int value = 100;
    [SyncVar] private bool isArtifact = false;

    public bool CanGuardsInteract => false;
    public bool IsArtifact => isArtifact;

    public void SetValue(int newValue) => value = newValue;
    public void SetIsArtifact(bool artifact) => isArtifact = artifact;

    public string GetInteractionText(PlayerInventory inventory)
    {
        if (!inventory.HasFreeSlot()) 
            return "Нет свободного места";
        return $"Нажмите E, чтобы взять {itemName} (Стоимость: {value})";
    }

    public bool CanInteract(PlayerInventory inventory) => inventory.HasFreeSlot();

    public void Interact(PlayerInventory inventory)
    {
        inventory.CmdAddItem(value);
        PlayerAudioManager playerAudio = inventory.GetComponent<PlayerAudioManager>();
        if (playerAudio != null) playerAudio.PlayPickupLocal();

        CmdDestroySelf();
    }

    [Command(requiresAuthority = false)]
    private void CmdDestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }
}