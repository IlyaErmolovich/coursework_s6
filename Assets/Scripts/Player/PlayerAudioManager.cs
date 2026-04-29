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
    public AudioClip swingAirSound;    // Взмах
    public AudioClip hitTargetSound;   // Попадание по врагу
    public AudioClip itemPickupSound;  // Сбор предмета

    // --- ШАГИ (Сетевые) ---
    [Command(requiresAuthority = false)]
    public void CmdPlayFootstep() => RpcPlayFootstep();

    [ClientRpc]
    private void RpcPlayFootstep() {
        if (footstepSource != null && footstepSound != null)
            footstepSource.PlayOneShot(footstepSound);
    }

    // --- БОЙ (СЕТЕВОЙ) ---

    // 1. Сетевой взмах (теперь все слышат свист дубинки)
    [Command(requiresAuthority = false)]
    public void CmdPlaySwing() => RpcPlaySwing();

    [ClientRpc]
    private void RpcPlaySwing() {
        if (combatSource != null && swingAirSound != null)
            combatSource.PlayOneShot(swingAirSound);
    }

    // 2. Сетевой удар по цели (слышат все вокруг)
    // Этот метод вызываем на сервере в PlayerCombat
    [ClientRpc] 
    public void RpcPlayHitEffect() {
        if (combatSource != null && hitTargetSound != null)
            combatSource.PlayOneShot(hitTargetSound);
    }

    // --- ИНВЕНТАРЬ (Локально) ---
    public void PlayPickupLocal() {
        if (isLocalPlayer && inventorySource != null && itemPickupSound != null)
            inventorySource.PlayOneShot(itemPickupSound);
    }
}