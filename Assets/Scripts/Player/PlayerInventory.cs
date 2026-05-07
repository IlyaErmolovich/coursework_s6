using UnityEngine;
using Mirror;

public class PlayerInventory : NetworkBehaviour
{
    [Header("Stats")]
    [SyncVar] private int _totalMoney = 0;
    
    [Header("Slots")]
    [SerializeField] private int maxSlots = 3;
    [SyncVar] private int _occupiedSlots = 0;

    [Header("Interaction Settings")]
    public float interactDistance = 3f;
    public LayerMask interactLayer;

    public int TotalMoney => _totalMoney;
    public int OccupiedSlots => _occupiedSlots;
    public int MaxSlots => maxSlots;
    [SyncVar] private bool _hasArtifact = false;

    [Command]
    public void CmdSetHasArtifact(bool has) => _hasArtifact = has;

    public bool HasFreeSlot() => _occupiedSlots < maxSlots;

    [Command]
    public void CmdAddItem(int amount)
    {
        if (_occupiedSlots < maxSlots)
        {
            _occupiedSlots++;
            _totalMoney += amount;
            Debug.Log($"Предмет поднят! Слоты: {_occupiedSlots}/{maxSlots}. Деньги: {_totalMoney}");
        }
    }

    [Command]
    public void CmdDepositMoney(int amount)
    {
        if (_totalMoney <= 0 && !_hasArtifact) return;

        int deposited = _totalMoney;
        _totalMoney = 0;
        _occupiedSlots = 0;
        
        if (_hasArtifact)
        {
            _hasArtifact = false;
            if (TeamScoreManager.singleton != null)
                TeamScoreManager.singleton.AddArtifactDeposit();
        }

        if (TeamScoreManager.singleton != null && deposited > 0)
            TeamScoreManager.singleton.AddDeposit(deposited);
    }
}