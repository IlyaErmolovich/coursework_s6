public interface IInteractable
{
    string GetInteractionText(PlayerInventory inventory);
    void Interact(PlayerInventory inventory);
    bool CanInteract(PlayerInventory inventory);
    
    
    bool CanGuardsInteract { get; } 
}