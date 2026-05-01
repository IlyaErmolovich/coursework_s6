using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class GameNetworkManager : NetworkManager
{
    [Header("Командные префабы для игры")]
    public GameObject guardPrefab;
    public GameObject thiefPrefab;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // В лобби спавним базу (LobbyPlayer)
        // В игре OnServerSceneChanged заменит его автоматически
        base.OnServerAddPlayer(conn);
    }

    // Добавляем второй параметр (PlayerLobbyData lobbyData) в скобки
    private void ReplacePlayerForGame(NetworkConnectionToClient conn, PlayerLobbyData lobbyData)
    {
        GameObject prefabToSpawn = (lobbyData.currentTeam == PlayerTeam.Guards) ? guardPrefab : thiefPrefab;
        
        Transform startPos = GetStartPosition();
        GameObject gamePlayer = Instantiate(prefabToSpawn, 
            startPos != null ? startPos.position : Vector3.zero, 
            startPos != null ? startPos.rotation : Quaternion.identity);

        // Копируем имя и команду в нового персонажа
        var newLobbyData = gamePlayer.GetComponent<PlayerLobbyData>();
        if (newLobbyData != null)
        {
            newLobbyData.playerName = lobbyData.playerName;
            newLobbyData.currentTeam = lobbyData.currentTeam;
        }

        GameObject oldLobbyObject = conn.identity.gameObject;

        // Сначала меняем связь[cite: 3, 5]
        NetworkServer.ReplacePlayerForConnection(conn, gamePlayer, true);

        // Потом УДАЛЯЕМ старый объект, иначе он будет висеть как на скрине
        NetworkServer.Destroy(oldLobbyObject);
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        // Если мы перешли в сцену игры
        if (sceneName == "Game")
        {
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn.identity != null)
                {
                    PlayerLobbyData lobbyData = conn.identity.GetComponent<PlayerLobbyData>();
                    if (lobbyData != null)
                    {
                        ReplacePlayerForGame(conn, lobbyData);
                    }
                }
            }
        }
    }
}