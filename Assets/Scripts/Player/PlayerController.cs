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
    [SyncVar] private NetworkIdentity _escortedIdentity;
    private float _lastSprintTime;
    public float StaminaProgress => _currentStamina / maxStamina;

    [Header("Animations")]
    [SerializeField] private float animationSmoothness = 5f;
    public float AnimationSmoothness => animationSmoothness;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 1.5f; // Высота прыжка в метрах
    
    [Header("Cuffed Settings")]
    [SerializeField] private float stopDistance = 1.8f;  // Дистанция, на которой грабитель останавливается
    [SerializeField] private float followDistance = 1.5f; // На каком расстоянии от охранника он должен стоять
    [SerializeField] private float followSpeedMultiplier = 1.2f; // Насколько быстрее обычного он идет за охранником

    [Header("Visuals")]
    [SerializeField] private GameObject handcuffsModel;
    private bool _jumpRequested;

    [SyncVar(hook = nameof(OnCuffedChanged))] private bool _isCuffed;
    public bool IsCuffed => _isCuffed;
    [SyncVar] private NetworkIdentity _escortTarget;

    private CharacterController _controller;
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private Vector3 _velocity; 
    private float _cameraRotationX;
    [SyncVar] private bool _isStunned;
    public bool IsStunned => _isStunned;
    private bool _canSprint;
    public float CurrentSprintFactor => _canSprint ? sprintMultiplier : 1f;

    private Vector3 _lastPosition;
    private float _calculatedSpeed;

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
        
        // Добавьте это для теста:
        var lobbyData = GetComponent<PlayerLobbyData>();
        Debug.Log($"Игрок заспавнен. Команда: {lobbyData.currentTeam}");
    }

    private void Update()
    {
        // --- ЭТОТ БЛОК ВЫПОЛНЯЕТСЯ ТОЛЬКО ДЛЯ ТОГО, КТО УПРАВЛЯЕТ ПЕРСОНАЖЕМ ---
        if (isLocalPlayer)
        {
            // 1. Гравитация (только для себя, остальные синхронизируют позицию через NetworkTransform)
            ApplyGravity();

            // 2. Логика следования за охранником
            if (_isCuffed && _escortTarget != null)
            {
                HandleClientEscortLogic();
            }
            // 3. Обычное управление
            else if (!_isStunned)
            {
                UpdateSprintState();
                HandleStamina();
                ApplyMovement();
                ApplyLook();
            }
        }
    }

    // Метод обязательно должен быть public, чтобы JailDoor его видел
[ClientRpc]
public void RpcTeleport(Vector3 newPosition)
{
    // 1. Выключаем контроллер
    if (_controller == null) _controller = GetComponent<CharacterController>();
    if (_controller != null) _controller.enabled = false;

    // 2. Перемещаем
    transform.position = newPosition;

    // 3. Сообщаем сетевому компоненту о телепортации через SendMessage
    // Это сработает, даже если мы не прописываем тип NetworkTransform в коде
    SendMessage("OnTeleport", newPosition, SendMessageOptions.DontRequireReceiver);

    // 4. Включаем обратно через физический кадр
    StartCoroutine(ReEnableController());
}

    private System.Collections.IEnumerator ReEnableController()
    {
        yield return new WaitForFixedUpdate(); // Ждем один такт физики
        if (_controller != null) _controller.enabled = true;
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _velocity.y < 0) _velocity.y = -2f;
        _velocity.y += gravity * Time.deltaTime;
        
        // Двигаем только если мы локальный игрок или сервер (если нет Client Authority)
        // В нашем случае — только локальный игрок.
        _controller.Move(_velocity * Time.deltaTime);
    }

    private void HandleClientEscortLogic()
    {
        Vector3 targetPos = _escortTarget.transform.position;
        Vector3 offset = transform.position - targetPos;
        float currentDistance = offset.magnitude;

        // ПОВОРОТ
        Vector3 lookDir = targetPos - transform.position;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
        }

        // ПЕРЕМЕЩЕНИЕ
        // Если расстояние больше установленной дистанции остановки — идем к цели
        if (currentDistance > stopDistance) 
        {
            // Рассчитываем точку, в которую нужно прийти (на расстоянии followDistance от охранника)
            Vector3 moveDest = targetPos + offset.normalized * followDistance;
            Vector3 moveDirection = (moveDest - transform.position).normalized;
            
            _controller.Move(moveDirection * moveSpeed * followSpeedMultiplier * Time.deltaTime);
        }
    }

    [Server]
    public void SetCuffed(bool state, NetworkIdentity guard = null)
    {
        _isCuffed = state;
        _escortTarget = guard;
    }

    private void OnCuffedChanged(bool oldVal, bool newVal)
    {
        // 1. Обновляем слой анимации
        var anims = GetComponentInChildren<PlayerAnimations>();
        if (anims != null) anims.UpdateCuffedLayer(newVal);

        // 2. Включаем/выключаем саму модель
        if (handcuffsModel != null)
        {
            handcuffsModel.SetActive(newVal);
        }
    }

    [Server]
    public void SetEscorting(PlayerController target)
    {
        _escortedIdentity = target != null ? target.netIdentity : null;
    }

    // Метод для получения объекта того, кого ведем (используется в UI)
    public PlayerController GetEscortedPlayer()
    {
        if (_escortedIdentity == null) return null;
        return _escortedIdentity.GetComponent<PlayerController>();
    }

    private void UpdateSprintState()
    {
        // Проверяем команду через LobbyData
        var lobbyData = GetComponent<PlayerLobbyData>();
        if (lobbyData != null && lobbyData.currentTeam == PlayerTeam.Guards)
        {
            _canSprint = false; // Охранник никогда не бегает
            return;
        }

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
        // Если игрок уже находится в состоянии стана, выходим, чтобы не перезапускать таймер
        if (_isStunned) return; 

        _isStunned = true;
        // Убираем CancelInvoke, так как теперь метод не будет вызываться повторно во время стана
        Invoke(nameof(ResetStun), duration);
    }

    [Server]
    private void ResetStun() => _isStunned = false;

    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => _lookInput = value.Get<Vector2>();
    
    private void ApplyMovement()
    {
        // Если на земле, обнуляем накопленную вертикальную скорость
        if (_controller.isGrounded && _velocity.y < 0) _velocity.y = -2f;

        // ЛОГИКА ПРЫЖКА
        if (_jumpRequested && _controller.isGrounded)
        {
            // Формула импульса: v = sqrt(h * -2 * g)
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            
            // Вызываем запуск анимации взлета
            var anims = GetComponentInChildren<PlayerAnimations>();
            if (anims != null) anims.SetJumpTrigger();
        }
        _jumpRequested = false;

        // Твой текущий код горизонтального перемещения
        Vector3 targetDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        float currentSpeedMagnitude = new Vector3(_currentVelocity.x, 0, _currentVelocity.z).magnitude;
        float currentSprintFactor = _canSprint ? sprintMultiplier : 1f;
        float targetSpeed = moveSpeed * currentSprintFactor; 
        
        if (_moveInput.y < 0) targetSpeed *= backPedalMultiplier;
        if (_moveInput.sqrMagnitude == 0) targetSpeed = 0;

        float smoothSpeed = Mathf.Lerp(currentSpeedMagnitude, targetSpeed, acceleration * Time.deltaTime);
        _currentVelocity = targetDirection.normalized * smoothSpeed;
        _controller.Move(_currentVelocity * Time.deltaTime);

        // ПРИМЕНЕНИЕ ГРАВИТАЦИИ
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
        // Если курсор виден (меню открыто), игнорируем прокрутку оружия
        if (Cursor.visible) return;

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

    public void OnJump(InputValue value)
    {
        if (value.isPressed) _jumpRequested = true;
    }

    public bool IsInteractPressed
    {
        get
        {
            if (Keyboard.current == null) return false;
            return Keyboard.current.eKey.isPressed;
        }
    }
}