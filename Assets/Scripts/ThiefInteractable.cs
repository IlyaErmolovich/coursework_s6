using UnityEngine;
using Mirror;

public class ThiefInteractable : NetworkBehaviour, IInteractable
{
    // Ссылка на контроллер в корне префаба
    private PlayerController _rootController;

    void Start()
    {
        _rootController = GetComponentInParent<PlayerController>();
    }

    public bool CanGuardsInteract => true;

    public string GetInteractionText(PlayerInventory inv)
    {
        if (_rootController.IsCuffed) return "Закован";
        if (!_rootController.IsStunned) return "";
        return "Нажмите E, чтобы заковать";
    }

    public bool CanInteract(PlayerInventory inv) 
    {
        return _rootController.IsStunned && !_rootController.IsCuffed;
    }

    public void Interact(PlayerInventory inv)
    {
        // Передаем NetworkIdentity охранника
        CmdApplyCuffs(inv.GetComponent<NetworkIdentity>());
    }

    [Command(requiresAuthority = false)]
    void CmdApplyCuffs(NetworkIdentity guardIdentity)
    {
        _rootController.SetCuffed(true, guardIdentity);
        
        // Запрещаем охраннику использовать оружие[cite: 10]
        var guardEquipment = guardIdentity.GetComponent<PlayerEquipmentManager>();
        if (guardEquipment != null) guardEquipment.SetEquipmentAccess(false);
    }
}