using UnityEngine;
using Mirror;

public class DoorTransformSync : NetworkBehaviour, IInteractable
{
    [Header("Настройки двери")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float doorSpeed = 5f;

    [Header("Сетевая оптимизация (Sensitivity)")]
    [Tooltip("Минимальный угол поворота (в градусах) для отправки пакета")]
    [SerializeField] private float rotationSensitivity = 0.1f;

    [SyncVar] private bool _isOpen = false;

    // Храним последний отправленный поворот на сервере
    private Quaternion lastSentRotation;

    // Целевой поворот для клиента
    private Quaternion targetClientRotation;

    public bool CanGuardsInteract => true;
    public string GetInteractionText(PlayerInventory inventory) =>
        _isOpen ? "Нажмите E, чтобы закрыть" : "Нажмите E, чтобы открыть";
    public bool CanInteract(PlayerInventory inventory) => true;

    public void Interact(PlayerInventory inventory) => CmdToggle();

    [Command(requiresAuthority = false)]
    private void CmdToggle() => _isOpen = !_isOpen;

    private void Start()
    {
        lastSentRotation = transform.localRotation;
        targetClientRotation = transform.localRotation;
    }

    private void Update()
    {
        if (isServer)
        {
            // 1. СЕРВЕР: Вращает дверь физически/математически
            Quaternion targetRot = _isOpen ? Quaternion.Euler(0, openAngle, 0) : Quaternion.identity;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * doorSpeed);

            // 2. СЕРВЕР: Проверяет порог чувствительности (Sensitivity Threshold)
            CheckAndSendRotation();
        }
        else
        {
            // 3. КЛИЕНТ: Плавно сглаживает поворот к полученному с сервера (Blending)
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetClientRotation, Time.deltaTime * doorSpeed);
        }
    }

    [Server]
    private void CheckAndSendRotation()
    {
        // Вычисляем угол между текущим поворотом и последним отправленным пакетом
        float angleDelta = Quaternion.Angle(transform.localRotation, lastSentRotation);

        // Если дверь сместилась МЕНЬШЕ чем на порог sensitivity — НЕ отправляем пакет
        if (angleDelta < rotationSensitivity) return;

        // Фиксируем новый отправленный поворот
        lastSentRotation = transform.localRotation;

        // Отправляем пакет только при реальном движении
        RpcUpdateDoorRotation(transform.localRotation);
    }

    [ClientRpc]
    private void RpcUpdateDoorRotation(Quaternion newRotation)
    {
        if (isServer) return;
        targetClientRotation = newRotation;
    }
}
