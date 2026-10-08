using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Fires pooled network bullets and sends numeric combat events.</summary>
public class GarbageWeapon : NetworkBehaviour
{
    [SerializeField] private float bulletLifetime = 0.5f;
    [SerializeField] private int lastCombatEventId;
    [SerializeField] private int receivedCombatEvents;

    private NetworkBulletPool bulletPool;
    private int nextCombatEventId;

    public override void OnStartServer()
    {
        bulletPool = FindFirstObjectByType<NetworkBulletPool>();
    }

    private void Update()
    {
        if (!isServer || Keyboard.current == null ||
            !Keyboard.current.spaceKey.isPressed || bulletPool == null)
            return;

        FireShot();
    }

    private void FireShot()
    {
        if (!isServer || bulletPool == null)
            return;

        Vector3 spawnPosition = transform.position + new Vector3(
            Random.Range(-3f, 3f), 0f, Random.Range(1f, 5f));

        bulletPool.ServerFire(spawnPosition, bulletLifetime);

        // The integer identifies the combat event without allocating a string.
        RpcRecordShot(++nextCombatEventId);
    }

    [ClientRpc]
    private void RpcRecordShot(int eventId)
    {
        lastCombatEventId = eventId;
        receivedCombatEvents++;
    }
}

