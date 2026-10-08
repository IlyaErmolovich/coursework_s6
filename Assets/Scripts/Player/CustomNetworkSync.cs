using UnityEngine;
using System.Collections.Generic;
using Mirror;

public class CustomNetworkSync : NetworkBehaviour
{
    public struct Snapshot
    {
        public Vector3 position;
        public double timestamp;
    }

    private List<Snapshot> snapshotBuffer = new List<Snapshot>();

    [Header("Настройки сети")]
    [SerializeField] private float interpolationDelay = 0.15f;
    [SerializeField] private float sendRate = 0.05f;
    [SerializeField] private float maxExtrapolationTime = 0.5f;

    private float extrapolationTimer = 0f;
    private float sendTimer = 0f;

    private void Start()
    {
        if (isServer)
        {
            InvokeRepeating(nameof(SendSnapshot), 0f, sendRate);
        }
    }

    void Update()
    {
        // Локальный игрок отправляет свою позицию серверу
        if (isLocalPlayer && !isServer)
        {
            sendTimer += Time.deltaTime;

            if (sendTimer >= sendRate)
            {
                sendTimer = 0f;
                CmdSendPosition(transform.position);
            }

            return;
        }

        if (isServer) return;

        ExecuteNetworkSmoothing();
    }

    // Клиент -> сервер
    [Command(channel = Channels.Unreliable)]
    private void CmdSendPosition(Vector3 position)
    {
        transform.position = position;
    }

    // Сервер -> клиенты
    [Server]
    private void SendSnapshot()
    {
        RpcReceiveSnapshot(transform.position, NetworkTime.time);
    }

    [ClientRpc(channel = Channels.Unreliable)]
    private void RpcReceiveSnapshot(
        Vector3 incomingPosition,
        double serverTime)
    {
        if (isServer || isLocalPlayer) return;

        OnReceiveSnapshotFromServer(
            incomingPosition,
            serverTime
        );
    }

    public void OnReceiveSnapshotFromServer(
        Vector3 incomingPosition,
        double serverTime)
    {
        snapshotBuffer.Add(new Snapshot
        {
            position = incomingPosition,
            timestamp = serverTime
        });

        extrapolationTimer = 0f;
    }

    private void ExecuteNetworkSmoothing()
    {
        if (snapshotBuffer.Count < 2) return;

        double renderTime =
            NetworkTime.time - interpolationDelay;

        while (snapshotBuffer.Count > 2 &&
               snapshotBuffer[1].timestamp < renderTime)
        {
            snapshotBuffer.RemoveAt(0);
        }

        if (renderTime >= snapshotBuffer[0].timestamp &&
            renderTime <= snapshotBuffer[1].timestamp)
        {
            double targetTime =
                snapshotBuffer[1].timestamp -
                snapshotBuffer[0].timestamp;

            float t = targetTime > 0
                ? (float)((renderTime -
                           snapshotBuffer[0].timestamp)
                          / targetTime)
                : 0f;

            transform.position = Vector3.Lerp(
                snapshotBuffer[0].position,
                snapshotBuffer[1].position,
                t
            );
        }
        else if (renderTime >
                 snapshotBuffer[snapshotBuffer.Count - 1].timestamp)
        {
            ExecuteExtrapolation();
        }
    }

    private void ExecuteExtrapolation()
    {
        Snapshot last =
            snapshotBuffer[snapshotBuffer.Count - 1];

        Snapshot prev =
            snapshotBuffer[snapshotBuffer.Count - 2];

        double timeDiff =
            last.timestamp - prev.timestamp;

        if (timeDiff <= 0) return;

        Vector3 velocity =
            (last.position - prev.position) /
            (float)timeDiff;

        if (extrapolationTimer < maxExtrapolationTime)
        {
            transform.position +=
                velocity * Time.deltaTime;

            extrapolationTimer += Time.deltaTime;
        }
    }
}