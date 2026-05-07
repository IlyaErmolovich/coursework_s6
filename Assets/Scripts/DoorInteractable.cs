using UnityEngine;
using Mirror;

public class DoorInteractable : NetworkBehaviour, IInteractable
{
    [SyncVar] private bool _isOpen = false;
    [SerializeField] private float openAngle = 90f;

    
    public bool CanGuardsInteract => true; 

    public string GetInteractionText(PlayerInventory inventory) => 
        _isOpen ? "Нажмите E, чтобы закрыть" : "Нажмите E, чтобы открыть";

    public bool CanInteract(PlayerInventory inventory) => true;

    public void Interact(PlayerInventory inventory) => CmdToggle();

    [Command(requiresAuthority = false)]
    private void CmdToggle() => _isOpen = !_isOpen;

    void Update()
    {
        Quaternion target = _isOpen ? Quaternion.Euler(0, openAngle, 0) : Quaternion.identity;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * 5f);
    }
}