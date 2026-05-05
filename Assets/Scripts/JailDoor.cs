using UnityEngine;
using Mirror;

public class JailDoor : NetworkBehaviour, IInteractable
{
    [SerializeField] private Transform jailInsidePoint; // Точка внутри камеры
    [SyncVar] private bool _isOpen = false;
    public enum DoorState { Closed, Broken, LockedWithPrisoner }
    public DoorState currentState = DoorState.Closed;
    public bool CanGuardsInteract => true;

    public string GetInteractionText(PlayerInventory inventory)
    {
        var controller = inventory.GetComponent<PlayerController>();
        // Проверяем через наш новый метод, ведет ли охранник вора
        var escorted = controller.GetEscortedPlayer();

        if (escorted != null) 
            return "Нажмите E, чтобы посадить в карцер";
        
        return _isOpen ? "Нажмите E, чтобы закрыть" : "Нажмите E, чтобы открыть";
    }

    public bool CanInteract(PlayerInventory inventory) => true;

    public void Interact(PlayerInventory inventory)
    {
        var controller = inventory.GetComponent<PlayerController>();
        var captive = controller.GetEscortedPlayer();

        if (captive != null)
        {
            // Если ведем кого-то — вызываем команду тюрьмы
            CmdPutInJail(captive.netIdentity);
        }
        else
        {
            // Иначе просто открываем/закрываем
            CmdToggle();
        }
    }

    [Server]
    public void BreakDoor() 
    {
        if (currentState == DoorState.Closed)
        {
            currentState = DoorState.Broken;
            RpcOpenDoor(); // Анимация открытия
        }
    }

    [ClientRpc]
    private void RpcOpenDoor()
    {
        // Здесь твоя логика открытия (анимация или просто поворот)
        // Например: animator.SetBool("IsOpen", true);
        Debug.Log("Дверь взломана и открыта!");
    }

    [ClientRpc]
    private void RpcCloseDoor()
    {
        // Логика закрытия
        // Например: animator.SetBool("IsOpen", false);
        Debug.Log("Вор в тюрьме, дверь заперта!");
    }

    [Server]
    public void CloseWithPrisoner()
    {
        currentState = DoorState.LockedWithPrisoner;
        RpcCloseDoor(); // Анимация закрытия
    }

    [Command(requiresAuthority = false)]
    private void CmdPutInJail(NetworkIdentity captiveIdentity, NetworkConnectionToClient sender = null)
    {
        PlayerController captive = captiveIdentity.GetComponent<PlayerController>();
        // Тот, кто вызвал команду (охранник)
        PlayerController guard = sender.identity.GetComponent<PlayerController>();

        if (captive != null)
        {
            captive.RpcTeleport(jailInsidePoint.position); // Телепорт вора
            captive.SetCuffed(false, null); // Снимаем наручники
        }

        if (guard != null)
        {
            guard.SetEscorting(null); // Охранник больше никого не ведет
            var eq = guard.GetComponent<PlayerEquipmentManager>();
            if (eq != null) eq.SetEquipmentAccess(true); // Возвращаем оружие
        }
        
        _isOpen = false; // Закрываем дверь автоматически
    }

    [Command(requiresAuthority = false)]
    private void CmdToggle() => _isOpen = !_isOpen;

    void Update()
    {
        float targetAngle = _isOpen ? 90f : 0f;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(0, targetAngle, 0), Time.deltaTime * 5f);
    }
}