using Mirror;
using UnityEngine;

/// <summary>Installs the sandbox's custom bullet spawn handlers for each session.</summary>
[RequireComponent(typeof(NetworkBulletPool))]
public sealed class GCSandboxNetworkManager : NetworkManager
{
    private NetworkBulletPool BulletPool => GetComponent<NetworkBulletPool>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        BulletPool.StartServerPool();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        BulletPool.StartClientPool();
    }

    public override void OnStopClient()
    {
        BulletPool.StopClientPool();
        base.OnStopClient();
    }

    public override void OnStopServer()
    {
        BulletPool.StopServerPool();
        base.OnStopServer();
    }
}
