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
    [SerializeField] private float gravity = -9.81f; // Гравитация нужна всегда
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float sprintMultiplier = 1.5f;
    private bool _isSprinting; // Храним состояние

    [Header("Movement Fine Tuning")]
    [SerializeField] private float acceleration = 10f; // Скорость разгона
    [SerializeField] private float backPedalMultiplier = 0.55f; // Коэффициент скорости назад (60%)
    private Vector3 _currentVelocity; // Текущая расчетная скорость для плавности
    
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 25f; // Расход в сек.
    [SerializeField] private float staminaRegenRate = 15f;  // Реген в сек.
    [SerializeField] private float staminaRegenDelay = 1.5f; // Пауза перед регеном

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
    public float CurrentSprintFactor 
    {
        get 
        {
            if (_isSprinting && _moveInput.y > 0.1f && _currentStamina > 1f) 
                return sprintMultiplier;
            return 1f;
        }
    }

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _currentStamina = maxStamina;

        if (!isLocalPlayer)
        {
            // Отключаем камеру у чужих игроков
            if (cameraTransform != null) cameraTransform.gameObject.SetActive(false);
            // Отключаем сам контроллер, чтобы он не конфликтовал с сетевой синхронизацией
            _controller.enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        HandleStamina();
        ApplyMovement();
        ApplyLook();
    }

    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => _lookInput = value.Get<Vector2>();
    
    private void ApplyMovement()
    {
        if (_controller.isGrounded && _velocity.y < 0) _velocity.y = -2f;

        Vector3 targetDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;

        // Считаем коэффициент ускорения: Shift + идем вперед (W или W+диагонали)
        float currentSprintFactor = (_isSprinting && _moveInput.y > 0.1f) ? sprintMultiplier : 1f;

        float targetSpeed = moveSpeed * currentSprintFactor; // Применяем ускорение здесь
        
        if (_moveInput.y < 0) targetSpeed *= backPedalMultiplier;
        if (_moveInput.sqrMagnitude == 0) targetSpeed = 0;

        float currentSpeedMagnitude = new Vector3(_currentVelocity.x, 0, _currentVelocity.z).magnitude;
        float smoothSpeed = Mathf.Lerp(currentSpeedMagnitude, targetSpeed, acceleration * Time.deltaTime);

        _currentVelocity = targetDirection.normalized * smoothSpeed;
        _controller.Move(_currentVelocity * Time.deltaTime);

        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    private void ApplyLook()
    {
        // Умножаем на 0.01 (или даже 0.001), чтобы в инспекторе были целые числа
        float finalSensitivity = lookSensitivity * 0.01f; 

        _cameraRotationX -= _lookInput.y * finalSensitivity;
        _cameraRotationX = Mathf.Clamp(_cameraRotationX, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(_cameraRotationX, 0, 0);
        transform.Rotate(Vector3.up * (_lookInput.x * finalSensitivity));
    }

    private void HandleStamina()
    {
        // Проверяем: нажата кнопка (твоя переменная), идем вперед и есть стамина
        bool isActuallySprinting = _isSprinting && _moveInput.y > 0.1f && _currentStamina > 0;

        if (isActuallySprinting)
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