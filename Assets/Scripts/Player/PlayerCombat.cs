using UnityEngine;
using Mirror;

public class PlayerCombat : NetworkBehaviour
{
    private PlayerController _controller;
    private PlayerAnimations _playerAnims;
    private PlayerEquipmentManager _equipment;

    private void Start()
    {
        if (!isLocalPlayer) return;

        _controller = GetComponent<PlayerController>();
        _playerAnims = GetComponentInChildren<PlayerAnimations>();
        _equipment = GetComponent<PlayerEquipmentManager>();

        // Подписываемся на события ввода
        _controller.OnWeaponScrollEvent += HandleScroll;
        _controller.OnAttackEvent += HandleAttack;
    }

    private void HandleScroll(float scrollValue)
    {
        // ЗАПРЕТ: Если проигрывается анимация удара, выходим
        if (_playerAnims.IsPlayingAttack()) return;

        if (_equipment != null)
        {
            _equipment.NextWeapon();
        }
    }

    private void HandleAttack()
    {
        if (_equipment == null || !_equipment.IsAnyWeaponDrawn()) return;
        if (_playerAnims.IsPlayingAttack()) return;

        _playerAnims.CmdAttack(); // Здесь вызов без аргумента
    }
    
    private void OnDestroy()
    {
        // Отписываемся, чтобы не было утечек памяти
        if (_controller != null)
        {
            _controller.OnWeaponScrollEvent -= HandleScroll;
            _controller.OnAttackEvent -= HandleAttack;
        }
    }
}