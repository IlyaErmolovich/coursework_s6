using UnityEngine;
using Mirror;

public class PlayerAnimations : NetworkBehaviour
{
    private Animator _animator;
    private PlayerController _controller;
    private PlayerEquipmentManager _equipment;
    private Vector2 _currentAnimationInput;

    private int _weaponIdleLayer;
    private int _fullBodyAttackLayer;
    private int _upperBodyAttackLayer;
    private PlayerAudioManager _audio;
    private int _cuffedLayerIndex;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponentInParent<PlayerController>();
        _equipment = GetComponentInParent<PlayerEquipmentManager>();
        _audio = GetComponentInParent<PlayerAudioManager>();
        
        _weaponIdleLayer = _animator.GetLayerIndex("Weapon Idle");
        _fullBodyAttackLayer = _animator.GetLayerIndex("Full Body Attack");
        _upperBodyAttackLayer = _animator.GetLayerIndex("Upper Body Attack");
        _cuffedLayerIndex = _animator.GetLayerIndex("Cuffed Layer");
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            // Передаем bool параметр isGrounded в Аниматор
            _animator.SetBool("isGrounded", _controller.GetComponent<CharacterController>().isGrounded);

            if (!_controller.enabled || _controller.IsStunned)
            {
                StopLocomotion();
            }
            else
            {
                HandleLocomotion();
                HandleAnimationSpeed();
            }
        }
        
        HandleWeightsSmoothing();
        HandleAttackLayerBlending();
    }

    private void HandleAnimationSpeed()
    {
        float speedFactor = _controller.CurrentSprintFactor;
        
        _animator.SetFloat("SprintSpeed", speedFactor);
    }

    private void HandleLocomotion()
    {
        Vector2 targetInput = _controller.GetCurrentInput();
        float step = _controller.AnimationSmoothness * Time.deltaTime;
        _currentAnimationInput = Vector2.MoveTowards(_currentAnimationInput, targetInput, step);

        _animator.SetFloat("moveX", _currentAnimationInput.x);
        _animator.SetFloat("moveY", _currentAnimationInput.y);
    }

    private void StopLocomotion()
    {
        float step = _controller.AnimationSmoothness * Time.deltaTime;
        _currentAnimationInput = Vector2.MoveTowards(_currentAnimationInput, Vector2.zero, step);

        _animator.SetFloat("moveX", _currentAnimationInput.x);
        _animator.SetFloat("moveY", _currentAnimationInput.y);
        _animator.SetFloat("SprintSpeed", 1f);
    }

    private void HandleWeightsSmoothing()
    {
        float step = _controller.AnimationSmoothness * Time.deltaTime;
        float currentWeight = _animator.GetLayerWeight(_weaponIdleLayer);

        float targetWeight = _equipment.IsAnyWeaponDrawn() ? 1f : 0f;

        if (!Mathf.Approximately(currentWeight, targetWeight))
        {
            float newWeight = Mathf.MoveTowards(currentWeight, targetWeight, step);
            _animator.SetLayerWeight(_weaponIdleLayer, newWeight);
        }
    }

    public void SetJumpTrigger()
    {
        if (isLocalPlayer)
        {
            CmdJump(); // Локальный игрок просит сервер синхронизировать прыжок
        }
    }

    [Command]
    private void CmdJump()
    {
        RpcJump(); // Сервер говорит всем клиентам нажать на триггер
    }

    [ClientRpc]
    private void RpcJump()
    {
        // Этот код выполнится у всех игроков на их экранах
        _animator.SetTrigger("jump");
    }

    [Command]
    public void CmdAttack()
    {
        RpcAttack();
    }

    [ClientRpc]
    private void RpcAttack()
    {
        _animator.SetTrigger("attack");
    }

    public bool IsPlayingAttack()
    {
        bool fullBodyAttack = _animator.GetCurrentAnimatorStateInfo(_fullBodyAttackLayer).IsTag("Attack");
        bool upperBodyAttack = _animator.GetCurrentAnimatorStateInfo(_upperBodyAttackLayer).IsTag("Attack");
        
        bool inTransition = _animator.IsInTransition(_fullBodyAttackLayer) || _animator.IsInTransition(_upperBodyAttackLayer);

        return fullBodyAttack || upperBodyAttack || inTransition;
    }

    private void HandleAttackLayerBlending()
    {
        if (!IsPlayingAttack()) return;

        bool isMoving = _controller.GetCurrentInput().magnitude > 0.1f;
        float step = _controller.AnimationSmoothness * Time.deltaTime;

        if (isMoving)
        {
            _animator.SetLayerWeight(_fullBodyAttackLayer, Mathf.MoveTowards(_animator.GetLayerWeight(_fullBodyAttackLayer), 0f, step));
            _animator.SetLayerWeight(_upperBodyAttackLayer, Mathf.MoveTowards(_animator.GetLayerWeight(_upperBodyAttackLayer), 1f, step));
        }
        else
        {
            _animator.SetLayerWeight(_fullBodyAttackLayer, Mathf.MoveTowards(_animator.GetLayerWeight(_fullBodyAttackLayer), 1f, step));
            _animator.SetLayerWeight(_upperBodyAttackLayer, Mathf.MoveTowards(_animator.GetLayerWeight(_upperBodyAttackLayer), 0f, step));
        }
    }

    public void UpdateCuffedLayer(bool active)
    {
        _animator.SetLayerWeight(_cuffedLayerIndex, active ? 1f : 0f);
    }   

    public void OnFootstep()
    {
        if (isLocalPlayer && _audio != null)
        {
            _audio.CmdPlayFootstep();
        }
    }

    public void OnSwingAttack()
    {
        if (!isLocalPlayer) return;

        if (_audio == null) _audio = GetComponentInParent<PlayerAudioManager>();

        if (_audio != null)
        {
            _audio.CmdPlaySwing();
        }
    }
}