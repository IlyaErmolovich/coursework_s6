using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerCombat _combat;

    void Start()
    {
        // Ищем PlayerCombat на родительском объекте (корне)
        _combat = GetComponentInParent<PlayerCombat>();
    }

    // Этот метод указываем в Animation Event на клипе удара
    public void OnHitAnimationEvent()
    {
        if (_combat != null) 
        {
            _combat.ProcessStunHit();
            
            // Добавляем звук взмаха/удара прямо сюда
            if (_combat.TryGetComponent(out PlayerAudioManager audio))
            {
                audio.CmdPlaySwing(); 
            }
        }
    }
}