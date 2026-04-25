using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Mirror;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    public System.Action<float> OnWeaponScrollEvent;
    public System.Action OnAttackEvent;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float lookSensitivity = 10f;
    [SerializeField] private float gravity = -9.81f; // Гравитация нужна всегда
    [SerializeField] private Transform cameraTransform;

    [Header("Movement Fine Tuning")]
    [SerializeField] private float acceleration = 10f; // Скорость разгона
    [SerializeField] private float backPedalMultiplier = 0.55f; // Коэффициент скорости назад (60%)
    private Vector3 _currentVelocity; // Текущая расчетная скорость для плавности
    
    [Header("Animations")]
    [SerializeField] private float animationSmoothness = 5f;
    public float AnimationSmoothness => animationSmoothness;


    private CharacterController _controller;
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private Vector3 _velocity; 
    private float _cameraRotationX;
    private bool _isCursorLocked = true;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();

        if (!isLocalPlayer)
        {
            // Отключаем камеру у чужих игроков
            if (cameraTransform != null) cameraTransform.gameObject.SetActive(false);
            // Отключаем сам контроллер, чтобы он не конфликтовал с сетевой синхронизацией
            _controller.enabled = false;
            return;
        }

        ToggleCursor(true);
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        HandleCursorLogic();
        
        if (_isCursorLocked)
        {
            ApplyMovement();
            ApplyLook();
        }
    }

    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => _lookInput = value.Get<Vector2>();
    
    public void OnToggleCursor(InputValue value)
    {
        if (value.isPressed) ToggleCursor(!_isCursorLocked);
    }

    private void ApplyMovement()
    {
        // Гравитация
        if (_controller.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; 
        }

        // 1. Считаем чистое направление из ввода
        Vector3 targetDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;

        // 2. Считаем целевую скорость (только назад режем коэффициент)
        float targetSpeed = moveSpeed;
        if (_moveInput.y < 0) 
        {
            targetSpeed *= backPedalMultiplier;
        }

        // Если ввода нет, целевая скорость 0
        if (_moveInput.sqrMagnitude == 0) targetSpeed = 0;

        // 3. Плавный разгон именно ВЕЛИЧИНЫ скорости (float), а не вектора
        // Это уберет "дрифт" на поворотах, но оставит плавный старт/стоп
        float currentSpeedMagnitude = new Vector3(_currentVelocity.x, 0, _currentVelocity.z).magnitude;
        float smoothSpeed = Mathf.Lerp(currentSpeedMagnitude, targetSpeed, acceleration * Time.deltaTime);

        // 4. Формируем итоговый вектор: новое направление * плавная скорость
        _currentVelocity = targetDirection.normalized * smoothSpeed;
        
        // Двигаем
        _controller.Move(_currentVelocity * Time.deltaTime);

        // Гравитация
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
    
    private void HandleCursorLogic()
    {
        if (!_isCursorLocked && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                ToggleCursor(true);
            }
        }
    }

    private void ToggleCursor(bool lockIt)
    {
        _isCursorLocked = lockIt;
        Cursor.lockState = lockIt ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockIt;
    }

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
}