using UnityEngine;
using Mirror;
using System.Collections;

public class JailDoor : NetworkBehaviour, IInteractable
{
    [Header("Door Visual")]
    [SerializeField] private float openAngle = 90f;
    [SyncVar] private bool _isOpen = true;  

    public enum DoorState { Closed, Broken, LockedWithPrisoner }
    [SyncVar] private DoorState _currentState = DoorState.Closed;

    [Header("Hacking Settings")]
    [SerializeField] private float holdDuration = 2f;

    [Header("Prison Zone")]
    [SerializeField] private Collider prisonZone;       

    [Header("References")]
    [SerializeField] private Transform jailInsidePoint; 

    
    public static System.Action<float> OnHackProgressChanged;

    [SyncVar] private int _prisonerCount;

    private Coroutine _hackCoroutine;
    private bool _isHacking = false;

    public bool CanGuardsInteract => true;

    #region IInteractable

    public string GetInteractionText(PlayerInventory inv)
    {
        var controller = inv.GetComponent<PlayerController>();
        var lobbyData = inv.GetComponent<PlayerLobbyData>();
        bool isGuard = lobbyData != null && lobbyData.currentTeam == PlayerTeam.Guards;

        if (isGuard)
        {
            if (controller.GetEscortedPlayer() != null) 
                return "Нажмите E, чтобы посадить в карцер";
            return "";
        }

        if (_currentState == DoorState.LockedWithPrisoner && !IsInsidePrisonZone(controller))
            return $"Зажмите E, чтобы взломать ({holdDuration} сек)";

        return "";
    }

    public bool CanInteract(PlayerInventory inv)
    {
        var controller = inv.GetComponent<PlayerController>();
        var lobbyData = inv.GetComponent<PlayerLobbyData>();
        bool isGuard = lobbyData != null && lobbyData.currentTeam == PlayerTeam.Guards;

        if (isGuard)
        {
            
            return controller.GetEscortedPlayer() != null;
        }
        else
        {
            return _currentState == DoorState.LockedWithPrisoner && !IsInsidePrisonZone(controller) && !_isHacking;
        }
    }

    public void Interact(PlayerInventory inv)
    {
        var controller = inv.GetComponent<PlayerController>();
        var lobbyData = inv.GetComponent<PlayerLobbyData>();
        bool isGuard = lobbyData != null && lobbyData.currentTeam == PlayerTeam.Guards;
        
        Debug.Log($"🎮 Interact вызван: isGuard={isGuard}, InsidePrisonZone={IsInsidePrisonZone(controller)}");

        
        if (!isGuard && IsInsidePrisonZone(controller))
        {
            Debug.Log("Изнутри карцера нельзя взаимодействовать с дверью!");
            return;
        }

        if (isGuard)
        {
            var captive = controller.GetEscortedPlayer();
            if (captive != null)
                CmdPutInJail(captive.netIdentity);
        }
        else
        {
            if (_hackCoroutine != null) StopCoroutine(_hackCoroutine);
            _hackCoroutine = StartCoroutine(HackRoutine(controller));
        }
    }

    private IEnumerator HackRoutine(PlayerController player)
    {
        _isHacking = true;
        float elapsed = 0f;
        OnHackProgressChanged?.Invoke(0f);

        while (elapsed < holdDuration)
        {
            if (!player.IsInteractPressed)
            {
                OnHackProgressChanged?.Invoke(-1f);
                _isHacking = false;
                yield break;
            }
            elapsed += Time.deltaTime;
            OnHackProgressChanged?.Invoke(elapsed / holdDuration);
            yield return null;
        }

        OnHackProgressChanged?.Invoke(-1f);
        _isHacking = false;
        CmdCompleteHack();
    }

    #endregion

    #region Commands & Server

    [Command(requiresAuthority = false)]
    private void CmdCompleteHack(NetworkConnectionToClient sender = null)
    {
        PlayerController thief = sender.identity.GetComponent<PlayerController>();
        if (thief == null) return;

        if (_currentState == DoorState.LockedWithPrisoner && !IsInsidePrisonZone(thief))
            BreakDoor();
    }

    [Command(requiresAuthority = false)]
    private void CmdPutInJail(NetworkIdentity captiveIdentity, NetworkConnectionToClient sender = null)
    {
        PlayerController captive = captiveIdentity.GetComponent<PlayerController>();
        PlayerController guard = sender.identity.GetComponent<PlayerController>();

        if (captive != null)
        {
            captive.RpcTeleport(jailInsidePoint.position);
            captive.SetCuffed(false, null);
            _prisonerCount++;
            var gm = FindObjectOfType<GameManager>();
            if (gm != null) gm.OnThiefImprisoned();
            RpcUpdatePrisonerCount(_prisonerCount);
        }

        if (guard != null)
        {
            guard.SetEscorting(null);
            var eq = guard.GetComponent<PlayerEquipmentManager>();
            if (eq != null) eq.SetEquipmentAccess(true);
        }

        _isOpen = false;
        _currentState = DoorState.LockedWithPrisoner;
    }

    [Server]
    public void BreakDoor()
    {
        if (_currentState == DoorState.LockedWithPrisoner)
        {
            _currentState = DoorState.Broken;
            _isOpen = true;
            RpcDoorBroken();
        }
    }

    [ClientRpc]
    private void RpcDoorBroken()
    {
        Debug.Log("Дверь карцера взломана и открыта!");
    }

    #endregion

    #region Prison Zone & Counting

    private bool IsInsidePrisonZone(PlayerController player)
    {
        if (prisonZone == null)
        {
            Debug.LogError("Prison Zone НЕ НАЗНАЧЕН!");
            return false;
        }
        
        bool inside = prisonZone.bounds.Contains(player.transform.position);
        Debug.Log($"CHECK: player pos={player.transform.position}, bounds={prisonZone.bounds}, inside={inside}");
        
        
        Collider[] hits = Physics.OverlapSphere(player.transform.position, 0.1f);
        bool foundTrigger = System.Array.Exists(hits, c => c == prisonZone);
        Debug.Log($"Player is inside trigger collider via OverlapSphere: {foundTrigger}");
        
        return inside;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;
        if (!other.CompareTag("Player")) return;

        var lobbyData = other.GetComponent<PlayerLobbyData>();
        if (lobbyData != null && lobbyData.currentTeam == PlayerTeam.Thieves)
        {
            _prisonerCount++;
            RpcUpdatePrisonerCount(_prisonerCount);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isServer) return;
        if (!other.CompareTag("Player")) return;
        
        var lobbyData = other.GetComponent<PlayerLobbyData>();
        if (lobbyData != null && lobbyData.currentTeam == PlayerTeam.Thieves)
        {
            _prisonerCount = Mathf.Max(0, _prisonerCount - 1);
            var gm = FindObjectOfType<GameManager>();
            if (gm != null) gm.OnThiefEscaped();
            
            if (_prisonerCount == 0 && _currentState == DoorState.Broken)
            {
                _currentState = DoorState.Closed;
                _isOpen = true;
            }
        }
    }

    [ClientRpc]
    private void RpcUpdatePrisonerCount(int count)
    {
        Debug.Log($"В карцере {count} грабителей");
    }

    public int GetPrisonerCount() => _prisonerCount;

    #endregion

    #region Visual

    private void Update()
    {
        float targetAngle = _isOpen ? openAngle : 0f;
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            Quaternion.Euler(0, targetAngle, 0),
            Time.deltaTime * 5f
        );
    }

    #endregion
}