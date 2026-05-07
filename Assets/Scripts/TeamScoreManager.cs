
using UnityEngine;
using Mirror;

public class TeamScoreManager : NetworkBehaviour
{
    public static TeamScoreManager singleton;

    [SyncVar] private int _totalDeposited = 0;
    public int TotalDeposited => _totalDeposited;

    
    public static System.Action<int> OnDepositUpdated;

    [SyncVar] private int _artifactsDeposited = 0;
    public int ArtifactsDeposited => _artifactsDeposited;
    public static System.Action<int> OnArtifactDepositUpdated;

    private void Awake()
    {
        if (singleton == null) singleton = this;
        else Destroy(gameObject);
    }

    [Server]
    public void AddDeposit(int amount)
    {
        _totalDeposited += amount;
        
        RpcUpdateDepositUI(_totalDeposited);
    }

    [ClientRpc]
    private void RpcUpdateDepositUI(int newTotal)
    {
        OnDepositUpdated?.Invoke(newTotal);
    }

    [Server]
    public void AddArtifactDeposit()
    {
        _artifactsDeposited++;
        RpcUpdateArtifactUI(_artifactsDeposited);
    }

    [ClientRpc]
    private void RpcUpdateArtifactUI(int newTotal)
    {
        OnArtifactDepositUpdated?.Invoke(newTotal);
    }
}