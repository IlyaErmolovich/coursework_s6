using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Separate server and remote-client pools for the GC sandbox.
/// NetworkServer.UnSpawn resets the server identity; the custom client handlers
/// reuse visual objects on remote clients. Host mode shares the server object.
/// </summary>
public sealed class NetworkBulletPool : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField, Min(1)] private int prewarmCount = 512;

    private struct ActiveBullet
    {
        public GameObject gameObject;
        public float expiresAt;
    }

    private Queue<GameObject> serverAvailable;
    private Queue<GameObject> clientAvailable;
    private List<ActiveBullet> serverActive;
    private uint bulletAssetId;
    private bool clientHandlerRegistered;

    public int ServerActiveCount => serverActive == null ? 0 : serverActive.Count;
    public int ServerAvailableCount => serverAvailable == null ? 0 : serverAvailable.Count;
    public int ClientAvailableCount => clientAvailable == null ? 0 : clientAvailable.Count;
    public int ServerCreatedCount { get; private set; }
    public int ServerReusedCount { get; private set; }
    public int ClientCreatedCount { get; private set; }
    public int ClientReusedCount { get; private set; }

    public void StartServerPool()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("GCSandbox: NetworkBullet prefab is missing.");
            return;
        }

        serverAvailable = new Queue<GameObject>(prewarmCount);
        serverActive = new List<ActiveBullet>(prewarmCount);
        ServerCreatedCount = 0;
        ServerReusedCount = 0;

        for (int i = 0; i < prewarmCount; i++)
            serverAvailable.Enqueue(CreateBullet(true));
    }

    public void StartClientPool()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("GCSandbox: NetworkBullet prefab is missing.");
            return;
        }

        bulletAssetId = bulletPrefab.GetComponent<NetworkIdentity>().assetId;
        NetworkClient.RegisterSpawnHandler(bulletAssetId, SpawnClientBullet, UnspawnClientBullet);
        clientHandlerRegistered = true;

        // A Host uses the very same GameObjects as its server. It must not
        // create or enqueue separate client copies.
        if (NetworkClient.activeHost)
            return;

        clientAvailable = new Queue<GameObject>(prewarmCount);
        ClientCreatedCount = 0;
        ClientReusedCount = 0;

        for (int i = 0; i < prewarmCount; i++)
            clientAvailable.Enqueue(CreateBullet(false));
    }

    public void ServerFire(Vector3 position, float lifetime)
    {
        if (!NetworkServer.active || serverAvailable == null)
            return;

        GameObject bullet;
        if (serverAvailable.Count > 0)
        {
            bullet = serverAvailable.Dequeue();
            ServerReusedCount++;
        }
        else
        {
            // Only occurs above the prewarmed simultaneous-bullet capacity.
            bullet = CreateBullet(true);
        }

        bullet.transform.SetPositionAndRotation(position, Quaternion.identity);
        bullet.SetActive(true);
        NetworkServer.Spawn(bullet);
        serverActive.Add(new ActiveBullet
        {
            gameObject = bullet,
            expiresAt = Time.time + lifetime
        });
    }

    private void Update()
    {
        if (!NetworkServer.active || serverActive == null)
            return;

        float now = Time.time;
        for (int i = serverActive.Count - 1; i >= 0; i--)
        {
            if (now < serverActive[i].expiresAt)
                continue;

            GameObject bullet = serverActive[i].gameObject;
            NetworkServer.UnSpawn(bullet);

            // UnSpawn deactivates and resets NetworkIdentity. Its Host client
            // callback deliberately does not enqueue this shared object.
            serverAvailable.Enqueue(bullet);
            int last = serverActive.Count - 1;
            serverActive[i] = serverActive[last];
            serverActive.RemoveAt(last);
        }
    }

    private GameObject SpawnClientBullet(SpawnMessage message)
    {
        GameObject bullet;
        if (clientAvailable != null && clientAvailable.Count > 0)
        {
            bullet = clientAvailable.Dequeue();
            ClientReusedCount++;
        }
        else
        {
            bullet = CreateBullet(false);
        }

        bullet.transform.SetPositionAndRotation(message.position, message.rotation);
        bullet.SetActive(true);
        return bullet;
    }

    private void UnspawnClientBullet(GameObject bullet)
    {
        if (NetworkClient.activeHost)
            return;

        bullet.SetActive(false);
        clientAvailable.Enqueue(bullet);
    }

    private GameObject CreateBullet(bool server)
    {
        GameObject bullet = Instantiate(bulletPrefab);
        bullet.SetActive(false);
        if (server)
            ServerCreatedCount++;
        else
            ClientCreatedCount++;
        return bullet;
    }

    public void StopClientPool()
    {
        if (clientHandlerRegistered)
        {
            NetworkClient.UnregisterSpawnHandler(bulletAssetId);
            clientHandlerRegistered = false;
        }

        if (clientAvailable != null)
        {
            while (clientAvailable.Count > 0)
                Destroy(clientAvailable.Dequeue());
            clientAvailable = null;
        }
    }

    public void StopServerPool()
    {
        if (serverActive != null)
        {
            for (int i = 0; i < serverActive.Count; i++)
            {
                GameObject bullet = serverActive[i].gameObject;
                if (bullet == null)
                    continue;
                if (NetworkServer.active)
                    NetworkServer.UnSpawn(bullet);
                Destroy(bullet);
            }
            serverActive.Clear();
            serverActive = null;
        }

        if (serverAvailable != null)
        {
            while (serverAvailable.Count > 0)
                Destroy(serverAvailable.Dequeue());
            serverAvailable = null;
        }
    }
}
