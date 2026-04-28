using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class StaminaUI : MonoBehaviour
{
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private GameObject fillArea; // Чтобы прятать полоску, когда полная

    private PlayerController _localController;

    void Update()
    {
        // Пытаемся найти локального игрока, если еще не нашли
        if (_localController == null)
        {
            if (NetworkClient.localPlayer != null)
                _localController = NetworkClient.localPlayer.GetComponent<PlayerController>();
            return;
        }

        // Обновляем слайдер
        float progress = _localController.StaminaProgress;
        staminaSlider.value = progress;
    }
}