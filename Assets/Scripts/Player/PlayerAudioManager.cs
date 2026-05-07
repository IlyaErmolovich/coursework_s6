using UnityEngine;
using Mirror;

public class PlayerAudioManager : NetworkBehaviour
{
    [Header("Источники (Audio Sources)")]
    public AudioSource footstepSource;
    public AudioSource combatSource; 
    public AudioSource inventorySource;

    [Header("Звуковые файлы (Audio Clips)")]
    public AudioClip footstepSound;
    public AudioClip swingAirSound;    
    public AudioClip hitTargetSound;  
    public AudioClip itemPickupSound; 

    [Command(requiresAuthority = false)]
    public void CmdPlayFootstep() => RpcPlayFootstep();

    [ClientRpc]
    private void RpcPlayFootstep() {
        if (footstepSource != null && footstepSound != null)
            footstepSource.PlayOneShot(footstepSound);
    }

    [Command(requiresAuthority = false)]
    public void CmdPlaySwing() => RpcPlaySwing();

    [ClientRpc]
    private void RpcPlaySwing() {
        if (combatSource != null && swingAirSound != null)
            combatSource.PlayOneShot(swingAirSound);
    }

    [ClientRpc] 
    public void RpcPlayHitEffect() {
        if (combatSource != null && hitTargetSound != null)
            combatSource.PlayOneShot(hitTargetSound);
    }

    public void PlayPickupLocal() {
        if (isLocalPlayer && inventorySource != null && itemPickupSound != null)
            inventorySource.PlayOneShot(itemPickupSound);
    }
}