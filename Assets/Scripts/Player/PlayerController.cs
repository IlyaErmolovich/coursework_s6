using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Mirror;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    public System.Action<float> OnWeaponScrollEvent;
    public System.Action OnAttackEvent;
    public System.Action OnInteractEvent;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float lookSensitivity = 10f;
    [SerializeField] private float gravity = -9.81f; 
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float sprintMultiplier = 1.5f;
    private bool _isSprinting; 

    [Header("Movement Fine Tuning")]
    [SerializeField] private float acceleration = 10f; 
    [SerializeField] private float backPedalMultiplier = 0.55f; 
    private Vector3 _currentVelocity; 
    
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 25f; 
    [SerializeField] private float staminaRegenRate = 15f;  
    [SerializeField] private float staminaRegenDelay = 1.5f; 

    [SyncVar] private float _currentStamina;
    private float _lastSprintTime;
    public float StaminaProgress => _currentStamina / maxStamina;

    [Header("Animations")]
    [SerializeField] private float animationSmoothness = 5f;
    public float AnimationSmoothness => animationSmoothness;

    private CharacterController _controller;
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private Vector3 _velocity; 
    private float _cameraRotationX;
    [SyncVar] private bool _isStunned;
    public bool IsStunned => _isStunned;
    private bool _canSprint;
    public float CurrentSprintFactor => _canSprint ? sprintMultiplier : 1f;

    [Server]
    public void DecreaseStamina(float amount)
    {
        _currentStamina = Mathf.Max(_currentStamina - amount, 0f);
        // Синхронизация произойдет автоматически через [SyncVar]
    }

    public override void OnStartLocalPlayer()
    {
        if (cameraTransform != null)
        {
            cameraTransform.gameObject.SetActive(true);
            cameraTransform.tag = "MainCamera";
        }
        if (_controller != null) _controller.enabled = true;
    }

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _currentStamina = maxStamina;

    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        if (_isStunned) 
        {
            _currentVelocity = Vector3.zero; 
            _canSprint = false; // Сбрасываем при стане
            return; 
        }

        // ВАЖНО: Сначала определяем, можем ли мы бежать
        UpdateSprintState(); 
        
        HandleStamina();
        ApplyMovement();
        ApplyLook();
    }

    private void UpdateSprintState()
    {
        // Условие 1: Нажата кнопка и игрок идет вперед
        bool isTryingToSprint = _isSprinting && _moveInput.y > 0.1f;

        // Условие 2: Если мы еще НЕ бежим, то начать можем ТОЛЬКО при 100% стамины
        if (!_canSprint)
        {
            if (isTryingToSprint && _currentStamina >= maxStamina - 0.1f)
            {
                _canSprint = true;
            }
        }
        else
        {
            // Условие 3: Если мы УЖЕ бежим, то прекращаем, если отпустили кнопку или стамина кончилась
            if (!isTryingToSprint || _currentStamina <= 0)
            {
                _canSprint = false;
            }
        }
    }

    [Server]
    public void Stun(float duration)
    {
        _isStunned = true;
        CancelInvoke(nameof(ResetStun)); 
        Invoke(nameof(ResetStun), duration);
    }

    [Server]
    private void ResetStun() => _isStunned = false;

    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => _lookInput = value.Get<Vector2>();
    
    private void ApplyMovement()
    {
        if (_controller.isGrounded && _velocity.y < 0) _velocity.y = -2f;

        Vector3 targetDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        float currentSpeedMagnitude = new Vector3(_currentVelocity.x, 0, _currentVelocity.z).magnitude;

        // Используем нашу переменную для определения скорости
        float currentSprintFactor = _canSprint ? sprintMultiplier : 1f;

        float targetSpeed = moveSpeed * currentSprintFactor; 
        if (_moveInput.y < 0) targetSpeed *= backPedalMultiplier;
        if (_moveInput.sqrMagnitude == 0) targetSpeed = 0;

        float smoothSpeed = Mathf.Lerp(currentSpeedMagnitude, targetSpeed, acceleration * Time.deltaTime);

        _currentVelocity = targetDirection.normalized * smoothSpeed;
        _controller.Move(_currentVelocity * Time.deltaTime);

        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    private void ApplyLook()
    {
        float finalSensitivity = lookSensitivity * 0.01f; 

        _cameraRotationX -= _lookInput.y * finalSensitivity;
        _cameraRotationX = Mathf.Clamp(_cameraRotationX, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(_cameraRotationX, 0, 0);
        transform.Rotate(Vector3.up * (_lookInput.x * finalSensitivity));
    }

    private void HandleStamina()
    {
        // Теперь тратим стамину только если бег РЕАЛЬНО разрешен
        if (_canSprint)
        {
            _currentStamina = Mathf.Max(_currentStamina - staminaDrainRate * Time.deltaTime, 0f);
            _lastSprintTime = Time.time;
            CmdSetStamina(_currentStamina);
        }
        else if (Time.time > _lastSprintTime + staminaRegenDelay && _currentStamina < maxStamina)
        {
            _currentStamina = Mathf.Min(_currentStamina + staminaRegenRate * Time.deltaTime, maxStamina);
            CmdSetStamina(_currentStamina);
        }
    }

    [Command]
    private void CmdSetStamina(float value) => _currentStamina = value;
    
    public Vector2 GetCurrentInput()
    {
        return _moveInput;
    }

    public void OnScroll(InputValue value)
    {
        float scrollY = value.Get<Vector2>().y;
        if (Mathf.Abs(scrollY) > 0.1f)
        {
            OnWeaponScrollEvent?.Invoke(scrollY);
        }
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            OnAttackEvent?.Invoke();
        }
    }

    public void OnInteract(InputValue value)
    {
        if (value.isPressed)
        {
            OnInteractEvent?.Invoke();
        }
    }

    public void OnSprint(InputValue value)
    {
        _isSprinting = value.isPressed;
    }
}