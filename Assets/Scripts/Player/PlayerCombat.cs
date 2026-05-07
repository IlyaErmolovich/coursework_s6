using UnityEngine;
using Mirror;

public class PlayerCombat : NetworkBehaviour
{
    [Header("Stun Settings")]
    [SerializeField] private float attackDistance = 2.0f;
    [SerializeField] private float attackRadius = 0.6f; 
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float stunHeight = 1.1f;    
    [SerializeField] private float stunDuration = 3.0f; 

    [Header("Stamina Hit Settings")]
    [SerializeField] private float staminaDamage = 30f; 

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

        _controller.OnWeaponScrollEvent += HandleScroll;
        _controller.OnAttackEvent += HandleAttack;
    }

    private void HandleScroll(float scrollValue)
    {
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

        if (_controller != null && _controller.IsStunned) return;

        if (Cursor.visible) return;

        _playerAnims.CmdAttack();
    }
    
    private void OnDestroy()
    {
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

        Vector3 origin = transform.position + Vector3.up * stunHeight; 
        Vector3 direction = transform.forward;

        if (Physics.SphereCast(origin, attackRadius, direction, out RaycastHit hit, attackDistance, playerLayer, QueryTriggerInteraction.Collide))
        {
            PlayerController victim = hit.collider.GetComponentInParent<PlayerController>();

            if (victim != null && victim.gameObject != gameObject)
            {
                
                CmdApplyCombatEffects(victim, stunDuration, staminaDamage);
            }
        }
    }

    
    
    
    
    

    
    

    
    
        
    
    
    
    
    [Command]
    private void CmdApplyCombatEffects(PlayerController target, float stunDur, float stamDamage)
    {
        if (target == null) return;

        
        target.DecreaseStamina(stamDamage);

        
        if (target.StaminaProgress <= 0)
        {
            target.Stun(stunDur);
        }
        
        
        if (_audio == null) _audio = GetComponent<PlayerAudioManager>();
        if (_audio != null) _audio.RpcPlayHitEffect(); 
    }
}