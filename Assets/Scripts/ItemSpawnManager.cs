using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;

public class ItemSpawnManager : NetworkBehaviour
{
    [Header("Unique Artifacts")]
    public GameObject[] uniqueArtifacts;

    [Header("Money Prefabs")]
    public GameObject[] moneyPrefabs;

    [Header("Money Value Range")]
    public int minMoneyValue = 50;
    public int maxMoneyValue = 300;

    private List<Transform> spawnPoints = new List<Transform>();

    public override void OnStartServer()
    {
        var spawnPointObjects = GameObject.FindGameObjectsWithTag("ItemSpawnPoint");
        foreach (var obj in spawnPointObjects)
            spawnPoints.Add(obj.transform);

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("Нет ItemSpawnPoint на сцене!");
            return;
        }

        
        spawnPoints = spawnPoints.OrderBy(x => Random.value).ToList();

        int idx = 0;

        
        foreach (var artifact in uniqueArtifacts)
        {
            if (idx >= spawnPoints.Count) break;
            SpawnItem(artifact, spawnPoints[idx], true, 0);
            idx++;
        }

        
        while (idx < spawnPoints.Count)
        {
            if (moneyPrefabs.Length == 0) break;
            GameObject money = moneyPrefabs[Random.Range(0, moneyPrefabs.Length)];
            int val = Random.Range(minMoneyValue, maxMoneyValue + 1);
            SpawnItem(money, spawnPoints[idx], false, val);
            idx++;
        }
    }

    private void SpawnItem(GameObject prefab, Transform point, bool isArtifact, int customValue)
    {
        GameObject item = Instantiate(prefab, point.position, point.rotation);
        PickupItem pickup = item.GetComponent<PickupItem>();
        if (pickup != null)
        {
            pickup.SetIsArtifact(isArtifact);
            if (customValue > 0) pickup.SetValue(customValue);
        }
        NetworkServer.Spawn(item);
    }
}