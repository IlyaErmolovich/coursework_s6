using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum PlayerTeam { None, Guards, Thieves }

public class PlayerLobbyData : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnTeamChanged))]
    public PlayerTeam currentTeam = PlayerTeam.None;

    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName = "Player";

    private void Awake()
    {
        
        DontDestroyOnLoad(gameObject);
    }

    public override void OnStartLocalPlayer()
    {
        string savedName = PlayerPrefs.GetString("PlayerName", "Player " + netId);
        CmdSetName(savedName);
        
        
        
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Lobby") 
        {
            CmdAutoAssignTeam();
        }
    }

    void OnNameChanged(string oldName, string newName)
    {
        PlayerNameTag nameTag = GetComponentInChildren<PlayerNameTag>();
        if (nameTag != null)
        {
            nameTag.UpdateDisplayName(newName);
        }
    }

    [Command]
    private void CmdAutoAssignTeam()
    {
        LobbyManager.singleton.AutoAssign(this);
    }

    [Command]
    public void CmdSwitchTeam()
    {
        PlayerTeam target = (currentTeam == PlayerTeam.Guards) ? PlayerTeam.Thieves : PlayerTeam.Guards;
        LobbyManager.singleton.TryChangeTeam(this, target);
    }

    [Command]
    public void CmdSetName(string name) 
    {
        playerName = name;
        if (LobbyUI.singleton != null) LobbyUI.singleton.RefreshUI();
    }

    [Command]
    public void CmdRequestTeamChange(PlayerTeam team)
    {
        LobbyManager.singleton.TryChangeTeam(this, team);
    }

    void OnTeamChanged(PlayerTeam oldTeam, PlayerTeam newTeam)
    {
        if (LobbyUI.singleton != null) LobbyUI.singleton.RefreshUI();
    }
    
    public override void OnStartClient()
    {
        Invoke(nameof(UpdateUIWithDelay), 0.1f);
    }

    void UpdateUIWithDelay()
    {
        if (LobbyUI.singleton != null) LobbyUI.singleton.RefreshUI();
    }

    private void OnDestroy()
    {
        if (LobbyUI.singleton != null && LobbyUI.singleton.gameObject != null) 
        {
            LobbyUI.singleton.RefreshUI();
        }
    }
}