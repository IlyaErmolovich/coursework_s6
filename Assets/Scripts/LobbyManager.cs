using UnityEngine;
using Mirror;
using System.Linq;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager singleton;

    [Header("Settings")]
    public int maxPlayersPerTeam = 4;

    private void Awake() => singleton = this;

    [Server]
    public void TryChangeTeam(PlayerLobbyData player, PlayerTeam targetTeam)
    {
        var allPlayers = FindObjectsByType<PlayerLobbyData>(FindObjectsSortMode.None);
        int count = allPlayers.Count(p => p.currentTeam == targetTeam);

        // Если место есть, меняем команду
        if (count < maxPlayersPerTeam)
        {
            player.currentTeam = targetTeam;
        }
    }

    public bool CanStart(out string message)
    {
        var players = FindObjectsByType<PlayerLobbyData>(FindObjectsSortMode.None);
        int guards = players.Count(p => p.currentTeam == PlayerTeam.Guards);
        int thieves = players.Count(p => p.currentTeam == PlayerTeam.Thieves);
        int noTeam = players.Count(p => p.currentTeam == PlayerTeam.None);

        if (noTeam > 0) { message = "Не все выбрали команду"; return false; }
        if (guards != thieves) { message = "Команды не равны!"; return false; }
        if (guards == 0) { message = "Нужно минимум по 1 игроку"; return false; }

        message = "Все готовы!";
        return true;
    }

    [Server]
    public void AutoAssign(PlayerLobbyData player)
    {
        var players = FindObjectsByType<PlayerLobbyData>(FindObjectsSortMode.None);
        int guards = players.Count(p => p.currentTeam == PlayerTeam.Guards);
        int thieves = players.Count(p => p.currentTeam == PlayerTeam.Thieves);

        player.currentTeam = (guards <= thieves) ? PlayerTeam.Guards : PlayerTeam.Thieves;
        
        // Принудительно обновляем UI на стороне сервера (хоста)
        if (LobbyUI.singleton != null) LobbyUI.singleton.RefreshUI();
    }
}