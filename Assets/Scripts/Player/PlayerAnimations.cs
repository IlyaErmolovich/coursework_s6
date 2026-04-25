using UnityEngine;
using Mirror;

public class PlayerAnimations : NetworkBehaviour
{    private Animator _animator;
    private PlayerController _controller;
    private Vector2 _currentAnimationInput;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        // Ищем контроллер на родительском объекте
        _controller = GetComponentInParent<PlayerController>();

        if (_controller == null)
        {
            Debug.LogError("PlayerAnimations не нашел PlayerController на родителе!");
        }

        if (!isLocalPlayer) return;
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        Vector2 targetInput = _controller.GetCurrentInput();

    float step = _controller.AnimationSmoothness * Time.deltaTime;

    _currentAnimationInput = Vector2.MoveTowards(
        _currentAnimationInput, 
        targetInput, 
        step
    );

        _animator.SetFloat("moveX", _currentAnimationInput.x);
        _animator.SetFloat("moveY", _currentAnimationInput.y);
    }
}