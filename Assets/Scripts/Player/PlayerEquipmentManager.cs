using UnityEngine;
using Mirror;

public class PlayerEquipmentManager : NetworkBehaviour
{
    [System.Serializable]
    public struct WeaponModel
    {
        public string name;
        public GameObject modelObject;
    }

    [SerializeField] private WeaponModel[] weapons;
    
    [SyncVar(hook = nameof(OnWeaponChanged))]
    private int _currentWeaponIndex = -1;

    [SyncVar] private bool _canUseEquipment = true;

    public void NextWeapon()
    {
        if (!_canUseEquipment) return; 
        if (weapons.Length == 0) return;
        
        int nextIndex = _currentWeaponIndex + 1;
        if (nextIndex >= weapons.Length) nextIndex = -1; 
        
        CmdSetWeapon(nextIndex);
    }

    [Command]
    private void CmdSetWeapon(int index) => _currentWeaponIndex = index;

    private void OnWeaponChanged(int oldIdx, int newIdx)
    {
        foreach (var weapon in weapons)
        {
            if (weapon.modelObject != null) weapon.modelObject.SetActive(false);
        }

        if (newIdx >= 0 && newIdx < weapons.Length)
        {
            if (weapons[newIdx].modelObject != null)
                weapons[newIdx].modelObject.SetActive(true);
        }
    }

    public bool IsAnyWeaponDrawn() => _currentWeaponIndex >= 0;

    [Server]
    public void SetEquipmentAccess(bool allowed)
    {
        _canUseEquipment = allowed;
        if (!allowed) CmdSetWeapon(-1); 
    }
}