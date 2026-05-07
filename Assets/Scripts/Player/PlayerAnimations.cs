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
    private Vector3 _lastPosition;
    private float _calculatedSpeed;

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
        
        
        float distanceMoved = Vector3.Distance(transform.position, _lastPosition);
        _calculatedSpeed = distanceMoved / Time.deltaTime;
        _lastPosition = transform.position;

        
        bool isGrounded = _controller.GetComponent<CharacterController>().enabled 
            ? _controller.GetComponent<CharacterController>().isGrounded 
            : true; 

        _animator.SetBool("isGrounded", isGrounded);

        if (_controller.IsCuffed)
        {
            
            HandleCuffedLocomotion();
        }
        else if (isLocalPlayer)
        {
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
        else
        {
            
            HandleProxyLocomotion();
        }
        
        HandleWeightsSmoothing();
        HandleAttackLayerBlending();
    } 

    private void HandleProxyLocomotion()
    {
        float targetY = (_calculatedSpeed > 0.1f) ? 1f : 0f;
        
        float step = _controller.AnimationSmoothness * Time.deltaTime;
        _currentAnimationInput.y = Mathf.MoveTowards(_currentAnimationInput.y, targetY, step);
        _currentAnimationInput.x = Mathf.MoveTowards(_currentAnimationInput.x, 0f, step);

        _animator.SetFloat("moveX", _currentAnimationInput.x);
        _animator.SetFloat("moveY", _currentAnimationInput.y);
    }

    private void HandleCuffedLocomotion()
    {
        
        float targetY = (_calculatedSpeed > 0.1f) ? 1f : 0f;
        
        float step = _controller.AnimationSmoothness * Time.deltaTime;
        _currentAnimationInput.y = Mathf.MoveTowards(_currentAnimationInput.y, targetY, step);
        _currentAnimationInput.x = Mathf.MoveTowards(_currentAnimationInput.x, 0f, step);

        _animator.SetFloat("moveX", _currentAnimationInput.x);
        _animator.SetFloat("moveY", _currentAnimationInput.y);
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
            CmdJump(); 
        }
    }

    [Command]
    private void CmdJump()
    {
        RpcJump(); 
    }

    [ClientRpc]
    private void RpcJump()
    {
        
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