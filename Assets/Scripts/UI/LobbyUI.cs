using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI singleton;

    [Header("Team Containers")]
    public Transform guardsContainer;
    public Transform thievesContainer;
    public GameObject playerNamePrefab;

    [Header("Controls")]
    public Button startButton;
    public TextMeshProUGUI statusText;

    private void Awake() => singleton = this;

    void Start()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (guardsContainer == null || thievesContainer == null) return; 

        foreach (Transform t in guardsContainer) 
        {
            if (t != null) Destroy(t.gameObject); 
        }
        foreach (Transform t in thievesContainer) 
        {
            if (t != null) Destroy(t.gameObject);
        }

        var allPlayers = FindObjectsByType<PlayerLobbyData>(FindObjectsSortMode.None);
        foreach (var p in allPlayers)
        {
            Transform target = null;
            if (p.currentTeam == PlayerTeam.Guards) target = guardsContainer;
            else if (p.currentTeam == PlayerTeam.Thieves) target = thievesContainer;

            if (target != null)
            {
                var label = Instantiate(playerNamePrefab, target).GetComponent<TextMeshProUGUI>();
                label.text = p.playerName;
            }
        }

        if (NetworkServer.active)
        {
            string msg;
            bool ready = LobbyManager.singleton.CanStart(out msg);
            startButton.interactable = ready;
            statusText.text = msg;
        }
        else
        {
            startButton.gameObject.SetActive(false);
            statusText.text = "Ожидание Хоста...";
        }
    }

    public void OnStartButtonClick()
    {
        NetworkManager.singleton.ServerChangeScene("Game");
    }

    public void OnSwitchTeamClick()
    {
        if (NetworkClient.localPlayer != null)
        {
            var data = NetworkClient.localPlayer.GetComponent<PlayerLobbyData>();
            data.CmdSwitchTeam();
        }
    }

    public void OnLeaveLobbyClick()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        else
        {
            NetworkManager.singleton.StopClient();
        }
    }
}