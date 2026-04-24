using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Mirror;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float gravity = -9.81f; // Гравитация нужна всегда
    [SerializeField] private Transform cameraTransform;

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
        // Простая гравитация, чтобы игрок не взлетал на кочках
        if (_controller.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; 
        }

        // Направление движения относительно поворота игрока
        Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        
        // Двигаем через CharacterController
        _controller.Move(move * moveSpeed * Time.deltaTime);

        // Применяем гравитацию
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    private void ApplyLook()
    {
        _cameraRotationX -= _lookInput.y * lookSensitivity;
        _cameraRotationX = Mathf.Clamp(_cameraRotationX, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(_cameraRotationX, 0, 0);
        transform.Rotate(Vector3.up * (_lookInput.x * lookSensitivity));
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
}