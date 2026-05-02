using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Linq;

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
        // 1. ОПРЕДЕЛЯЕМ ПРЕФАБ (проверьте еще раз: вор это Thieves, охранник это Guards)[cite: 3, 4]
        GameObject prefabToSpawn = (lobbyData.currentTeam == PlayerTeam.Thieves) ? thiefPrefab : guardPrefab;
        
        Transform startPos = GetStartPosition();
        GameObject gamePlayer = Instantiate(prefabToSpawn, 
            startPos != null ? startPos.position : Vector3.zero, 
            startPos != null ? startPos.rotation : Quaternion.identity);

        // 2. КОПИРУЕМ ДАННЫЕ[cite: 4]
        var newLobbyData = gamePlayer.GetComponent<PlayerLobbyData>();
        if (newLobbyData != null)
        {
            newLobbyData.playerName = lobbyData.playerName;
            newLobbyData.currentTeam = lobbyData.currentTeam; 
        }

        // 3. СПАВНИМ И ЗАМЕНЯЕМ[cite: 4]
        // Важно: Сохраняем ссылку на старый объект до замены
        GameObject oldLobbyObject = conn.identity.gameObject;

        // Сначала спавним новый объект для этого конкретного владельца[cite: 4]
        NetworkServer.Spawn(gamePlayer, conn);
        
        // Переключаем управление[cite: 4]
        NetworkServer.ReplacePlayerForConnection(conn, gamePlayer, true);

        // Удаляем старый объект, чтобы он не мешался[cite: 3, 4]
        NetworkServer.Destroy(oldLobbyObject);
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        if (sceneName == "Game")
        {
            // Используем ToList(), чтобы зафиксировать состояние подключений на этот момент[cite: 5]
            var connections = NetworkServer.connections.Values.ToList();
            
            foreach (var conn in connections)
            {
                // Проверяем, что у соединения есть объект и он живой
                if (conn != null && conn.identity != null)
                {
                    PlayerLobbyData oldData = conn.identity.GetComponent<PlayerLobbyData>();
                    if (oldData != null)
                    {
                        // Вызываем замену, передавая данные конкретного игрока
                        ReplacePlayerForGame(conn, oldData);
                    }
                }
            }
        }
    }
}