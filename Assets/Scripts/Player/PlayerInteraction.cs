using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private PlayerInventory _inventory;
    private PlayerController _controller;
    private Camera _cam;
    private IInteractable _currentInteractable;

    public IInteractable GetCurrentInteractable => _currentInteractable;

    private float _dist;
    private LayerMask _layer;

    void Start()
    {
        _cam = GetComponent<Camera>();
        _inventory = GetComponentInParent<PlayerInventory>();
        _controller = GetComponentInParent<PlayerController>();

        _dist = _inventory.interactDistance;
        _layer = _inventory.interactLayer;

        if (_controller != null)
            _controller.OnInteractEvent += HandleInteraction;
    }

    void Update()
    {
        if (!_inventory.isLocalPlayer) return;

        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, _dist, _layer))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                _currentInteractable = interactable;
                return;
            }
        }
        _currentInteractable = null;
    }

    private void HandleInteraction()
    {
        if (_currentInteractable != null && _currentInteractable.CanInteract(_inventory))
        {
            _currentInteractable.Interact(_inventory);
        }
    }
}