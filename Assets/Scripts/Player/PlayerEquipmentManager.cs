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
    
    // Синхронизируем индекс текущего оружия. 
    // При изменении вызывается метод ChangeWeaponLocal
    [SyncVar(hook = nameof(OnWeaponChanged))]
    private int _currentWeaponIndex = -1;

    public void NextWeapon()
    {
        if (weapons.Length == 0) return;
        
        int nextIndex = _currentWeaponIndex + 1;
        if (nextIndex >= weapons.Length) nextIndex = -1; // -1 = убрать всё
        
        CmdSetWeapon(nextIndex);
    }

    [Command]
    private void CmdSetWeapon(int index) => _currentWeaponIndex = index;

    private void OnWeaponChanged(int oldIdx, int newIdx)
    {
        // Выключаем всё оружие
        foreach (var weapon in weapons)
        {
            if (weapon.modelObject != null) weapon.modelObject.SetActive(false);
        }

        // Включаем только выбранное
        if (newIdx >= 0 && newIdx < weapons.Length)
        {
            if (weapons[newIdx].modelObject != null)
                weapons[newIdx].modelObject.SetActive(true);
        }
    }

    // Полезный метод для аниматора: достал ли игрок хоть что-то?
    public bool IsAnyWeaponDrawn() => _currentWeaponIndex >= 0;
}