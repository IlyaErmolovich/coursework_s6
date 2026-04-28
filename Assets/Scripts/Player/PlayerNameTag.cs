using UnityEngine;
using TMPro;
using Mirror;

public class PlayerNameTag : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    private Transform _mainCamTransform;

    void Start()
    {
        // Пробуем обновить при старте на всякий случай
        PlayerLobbyData data = GetComponentInParent<PlayerLobbyData>();
        if (data != null) UpdateDisplayName(data.playerName);
    }

    // Делаем метод публичным и принимающим имя
    public void UpdateDisplayName(string newName)
    {
        if (nameText != null && !string.IsNullOrEmpty(newName))
        {
            nameText.text = newName;
        }
    }

    void LateUpdate()
    {
        if (_mainCamTransform == null)
        {
            if (Camera.main != null) _mainCamTransform = Camera.main.transform;
            return;
        }

        // Билбординг
        transform.LookAt(transform.position + _mainCamTransform.forward);
    }
}