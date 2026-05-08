using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Linq;
using System.Collections.Generic;

public class GameNetworkManager : NetworkManager
{
    [Header("Командные префабы для игры")]
    public GameObject guardPrefab;
    public GameObject thiefPrefab;

    [Header("Spawn Points (будут найдены автоматически на сцене Game)")]
    private Transform[] guardSpawnPoints;
    private Transform[] thiefSpawnPoints;

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name == "Game" || (GameManager.singleton != null && GameManager.singleton.IsMatchActive))
        {
            Debug.Log("Игра уже идёт, подключение отклонено.");
            conn.Disconnect();
            return;
        }
        base.OnServerConnect(conn);
    }
    
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name == "Game")
        {
            Debug.Log("Игра уже начата, подключение запрещено.");
            return;
        }
        base.OnServerAddPlayer(conn);
    }

    
    private Transform GetTeamStartPosition(PlayerTeam team)
    {
        Transform[] points = (team == PlayerTeam.Guards) ? guardSpawnPoints : thiefSpawnPoints;
        if (points == null || points.Length == 0)
        {
            Debug.LogWarning($"Нет точек спавна для команды {team}!");
            return null;
        }

        
        List<Transform> available = new List<Transform>();
        foreach (var point in points)
        {
            bool occupied = false;
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn.identity != null && Vector3.Distance(conn.identity.transform.position, point.position) < 0.5f)
                {
                    occupied = true;
                    break;
                }
            }
            if (!occupied) available.Add(point);
        }

        if (available.Count == 0)
        {
            Debug.LogWarning($"Все точки для команды {team} заняты, берём первую попавшуюся");
            return points[0];
        }
        return available[Random.Range(0, available.Count)];
    }

    
    private void ReplacePlayerForGame(NetworkConnectionToClient conn, PlayerLobbyData lobbyData)
    {
        GameObject prefabToSpawn = (lobbyData.currentTeam == PlayerTeam.Thieves) ? thiefPrefab : guardPrefab;
        
        Transform startPos = GetTeamStartPosition(lobbyData.currentTeam);
        Vector3 spawnPos = startPos != null ? startPos.position : Vector3.zero;
        Quaternion spawnRot = startPos != null ? startPos.rotation : Quaternion.identity;

        GameObject gamePlayer = Instantiate(prefabToSpawn, spawnPos, spawnRot);

        var newLobbyData = gamePlayer.GetComponent<PlayerLobbyData>();
        if (newLobbyData != null)
        {
            newLobbyData.playerName = lobbyData.playerName;
            newLobbyData.currentTeam = lobbyData.currentTeam;
        }

        GameObject oldLobbyObject = conn.identity.gameObject;
        NetworkServer.Spawn(gamePlayer, conn);
        NetworkServer.ReplacePlayerForConnection(conn, gamePlayer, true);
        NetworkServer.Destroy(oldLobbyObject);
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        if (sceneName == "Game")
        {
            var guards = GameObject.FindGameObjectsWithTag("GuardSpawnPoint");
            guardSpawnPoints = guards.Select(go => go.transform).ToArray();
            var thieves = GameObject.FindGameObjectsWithTag("ThiefSpawnPoint");
            thiefSpawnPoints = thieves.Select(go => go.transform).ToArray();

            Debug.Log($"Найдено точек для охраны: {guardSpawnPoints.Length}, для воров: {thiefSpawnPoints.Length}");

            StartCoroutine(ReplacePlayersAfterDelay());
        }
    }

    private System.Collections.IEnumerator ReplacePlayersAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        
        var connections = NetworkServer.connections.Values.ToList();
        Debug.Log($"Замена игроков: всего подключений {connections.Count}");
        
        foreach (var conn in connections)
        {
            if (conn != null && conn.identity != null)
            {
                PlayerLobbyData oldData = conn.identity.GetComponent<PlayerLobbyData>();
                if (oldData != null)
                {
                    ReplacePlayerForGame(conn, oldData);
                    Debug.Log($"Игрок {oldData.playerName} заменён");
                }
            }
            else
            {
                Debug.LogWarning($"Соединение {conn?.connectionId} не имеет identity, возможно клиент не загрузился");
            }
        }
    }
}