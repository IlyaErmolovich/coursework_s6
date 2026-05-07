using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerCombat _combat;

    void Start()
    {
        _combat = GetComponentInParent<PlayerCombat>();
    }

    public void OnHitAnimationEvent()
    {
        if (_combat != null) 
        {
            _combat.ProcessStunHit();
            
            if (_combat.TryGetComponent(out PlayerAudioManager audio))
            {
                audio.CmdPlaySwing(); 
            }
        }
    }
}