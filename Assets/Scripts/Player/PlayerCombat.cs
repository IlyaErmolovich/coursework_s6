using UnityEngine;
using Mirror;

public class PlayerCombat : NetworkBehaviour
{
    [Header("Stun Settings")]
    [SerializeField] private float attackDistance = 2.0f;
    [SerializeField] private float attackRadius = 0.6f; 
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float stunHeight = 1.1f;    // Высота появления сферы
    [SerializeField] private float stunDuration = 3.0f;  // Длительность стана в секундах

    private PlayerController _controller;
    private PlayerAnimations _playerAnims;
    private PlayerEquipmentManager _equipment;
    private PlayerAudioManager _audio;


    private void Start()
    {
        if (!isLocalPlayer) return;

        _controller = GetComponent<PlayerController>();
        _playerAnims = GetComponentInChildren<PlayerAnimations>();
        _equipment = GetComponent<PlayerEquipmentManager>();
        _audio = GetComponentInParent<PlayerAudioManager>();

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
        // 1. Базовые проверки (оружие и не идет ли уже атака)
        if (_equipment == null || !_equipment.IsAnyWeaponDrawn()) return;
        if (_playerAnims.IsPlayingAttack()) return;

        // 2. ЗАПРЕТ: Если игрок оглушен, он не может начать атаку
        if (_controller != null && _controller.IsStunned) return;

        // 3. ЗАПРЕТ: Если открыто меню (курсор виден), бить нельзя
        if (Cursor.visible) return;

        _playerAnims.CmdAttack();
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

    public void ProcessStunHit()
    {
        if (!isLocalPlayer) return;

        if (_controller != null && _controller.IsStunned) return;

        // Используем переменную stunHeight для высоты
        Vector3 origin = transform.position + Vector3.up * stunHeight; 
        Vector3 direction = transform.forward;

        if (Physics.SphereCast(origin, attackRadius, direction, out RaycastHit hit, attackDistance, playerLayer, QueryTriggerInteraction.Collide))
        {
            PlayerController victim = hit.collider.GetComponentInParent<PlayerController>();

            if (victim != null && victim.gameObject != gameObject)
            {
                // Передаем переменную stunDuration на сервер
                CmdApplyStun(victim, stunDuration);
            }
        }
    }

    // private void OnDrawGizmos()
    // {
    //     // Отрисовка в редакторе тоже использует stunHeight
    //     Vector3 origin = transform.position + Vector3.up * stunHeight;
    //     Vector3 direction = transform.forward;

    //     Gizmos.color = Color.white;
    //     Gizmos.DrawRay(origin, direction * attackDistance);

    //     Vector3 sphereEndPos = origin + direction * attackDistance;
    //     Gizmos.DrawWireSphere(sphereEndPos, attackRadius);
        
    //     Gizmos.color = new Color(1, 0, 0, 0.2f);
    //     Gizmos.DrawSphere(sphereEndPos, attackRadius);
    // }
    
    [Command]
    private void CmdApplyStun(PlayerController target, float duration)
    {
        if (target == null) return;
        target.Stun(duration);
        
        // Пытаемся найти аудиоменеджер, если он еще не найден (на сервере он будет null)
        if (_audio == null) _audio = GetComponent<PlayerAudioManager>();

        if (_audio != null)
        {
            _audio.RpcPlayHitEffect(); 
        }
        else 
        {
            Debug.LogError("СЕРВЕР: Не найден PlayerAudioManager на игроке!");
        }
    }
}