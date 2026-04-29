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

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponentInParent<PlayerController>();
        _equipment = GetComponentInParent<PlayerEquipmentManager>();
        _audio = GetComponentInParent<PlayerAudioManager>();
        
        _weaponIdleLayer = _animator.GetLayerIndex("Weapon Idle");
        _fullBodyAttackLayer = _animator.GetLayerIndex("Full Body Attack");
        _upperBodyAttackLayer = _animator.GetLayerIndex("Upper Body Attack");
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            // Если контроллер выключен (МЕНЮ) или игрок ОГЛУШЕН
            if (!_controller.enabled || _controller.IsStunned)
            {
                // Плавно обнуляем анимацию, чтобы ноги не бежали на месте
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
        // Берем фактор ускорения напрямую из контроллера
        float speedFactor = _controller.CurrentSprintFactor;
        
        // Передаем в Animator. 
        // В Unity у стейта бега нажми "Multiplier" и выбери этот параметр
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
        // Используем AnimationSmoothness из контроллера для мягкой остановки
        float step = _controller.AnimationSmoothness * Time.deltaTime;
        _currentAnimationInput = Vector2.MoveTowards(_currentAnimationInput, Vector2.zero, step);

        _animator.SetFloat("moveX", _currentAnimationInput.x);
        _animator.SetFloat("moveY", _currentAnimationInput.y);
        _animator.SetFloat("SprintSpeed", 1f); // Возвращаем скорость анимации в дефолт
    }

    private void HandleWeightsSmoothing()
    {
        float step = _controller.AnimationSmoothness * Time.deltaTime;
        float currentWeight = _animator.GetLayerWeight(_weaponIdleLayer);

        // Если в менеджере выбран индекс >= 0, значит цель — 1 (рука поднята)
        float targetWeight = _equipment.IsAnyWeaponDrawn() ? 1f : 0f;

        if (!Mathf.Approximately(currentWeight, targetWeight))
        {
            float newWeight = Mathf.MoveTowards(currentWeight, targetWeight, step);
            _animator.SetLayerWeight(_weaponIdleLayer, newWeight);
        }
    }

    // Удар оставляем как есть, так как в аниматоре плавность 
    // настроена через переходы (Transitions)
    [Command]
    public void CmdAttack() // Убираем (bool moving) здесь
    {
        RpcAttack();
    }

    [ClientRpc]
    private void RpcAttack() // Убрали bool moving
    {
        _animator.SetTrigger("attack");
    }

    public bool IsPlayingAttack()
    {
        // Проверяем, находится ли аниматор в состоянии атаки на любом из слоев удара
        bool fullBodyAttack = _animator.GetCurrentAnimatorStateInfo(_fullBodyAttackLayer).IsTag("Attack");
        bool upperBodyAttack = _animator.GetCurrentAnimatorStateInfo(_upperBodyAttackLayer).IsTag("Attack");
        
        // Также проверяем, не находится ли он в процессе перехода (Transition) к атаке
        bool inTransition = _animator.IsInTransition(_fullBodyAttackLayer) || _animator.IsInTransition(_upperBodyAttackLayer);

        return fullBodyAttack || upperBodyAttack || inTransition;
    }

    private void HandleAttackLayerBlending()
    {
        // Проверяем, проигрывается ли сейчас атака
        if (!IsPlayingAttack()) return;

        // Определяем, движется ли персонаж в данный момент
        // Используем магнитуду ввода из контроллера
        bool isMoving = _controller.GetCurrentInput().magnitude > 0.1f;
        float step = _controller.AnimationSmoothness * Time.deltaTime;

        if (isMoving)
        {
            // На бегу: выключаем Full Body, включаем Upper Body
            _animator.SetLayerWeight(_fullBodyAttackLayer, Mathf.MoveTowards(_animator.GetLayerWeight(_fullBodyAttackLayer), 0f, step));
            _animator.SetLayerWeight(_upperBodyAttackLayer, Mathf.MoveTowards(_animator.GetLayerWeight(_upperBodyAttackLayer), 1f, step));
        }
        else
        {
            // На месте: включаем Full Body, выключаем Upper Body
            _animator.SetLayerWeight(_fullBodyAttackLayer, Mathf.MoveTowards(_animator.GetLayerWeight(_fullBodyAttackLayer), 1f, step));
            _animator.SetLayerWeight(_upperBodyAttackLayer, Mathf.MoveTowards(_animator.GetLayerWeight(_upperBodyAttackLayer), 0f, step));
        }
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

        // Если вдруг по какой-то причине _audio пустой (например, не успел найтись в Start)
        if (_audio == null) _audio = GetComponentInParent<PlayerAudioManager>();

        if (_audio != null)
        {
            _audio.CmdPlaySwing();
        }
    }
}