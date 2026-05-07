using UnityEngine;
using Mirror;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public static GameManager singleton;

    [Header("Match Settings")]
    [SerializeField] private float matchDuration = 600f;
    public int targetDepositScore = 5000;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TextMeshProUGUI victoryMessageText;

    [SyncVar] private float _timeLeft;
    [SyncVar] private bool _matchEnded = false;

    private int _totalThiefCount;
    private int _imprisonedThiefCount;

    public bool IsMatchEnded => _matchEnded;
    [SyncVar] private bool _isMatchActive = false;
    public bool IsMatchActive => _isMatchActive;

    private void Awake()
    {
        if (singleton == null) singleton = this;
        else Destroy(gameObject);
    }

    public override void OnStartServer()
    {
        _isMatchActive = true;
        _timeLeft = matchDuration;
        _totalThiefCount = CountThieves();
        _imprisonedThiefCount = 0;
        InvokeRepeating(nameof(ServerUpdateTimer), 0f, 1f);
    }

    private int CountThieves()
    {
        var players = FindObjectsOfType<PlayerLobbyData>();
        int count = 0;
        foreach (var p in players)
            if (p.currentTeam == PlayerTeam.Thieves) count++;
        return count;
    }

    [Server]
    private void ServerUpdateTimer()
    {
        if (_matchEnded) return;
        if (_timeLeft > 0)
        {
            _timeLeft -= 1f;
            RpcUpdateTimerUI(_timeLeft);
            if (_timeLeft <= 0) CheckTimeVictory();
        }
    }

    [Server]
    private void CheckTimeVictory()
    {
        if (_matchEnded) return;
        var ts = TeamScoreManager.singleton;
        int deposited = ts != null ? ts.TotalDeposited : 0;
        int artifacts = ts != null ? ts.ArtifactsDeposited : 0;

        if (deposited >= targetDepositScore && artifacts >= 1)
            EndMatch("Грабители победили! Собрана нужная сумма и артефакт.");
        else
            EndMatch("Охрана победила! Время вышло, сумма или артефакт не собраны.");
    }

    [Server]
    public void OnThiefImprisoned()
    {
        if (_matchEnded) return;
        _imprisonedThiefCount++;
        if (_imprisonedThiefCount >= _totalThiefCount && _totalThiefCount > 0)
            EndMatch("Охрана победила! Все грабители в карцере.");
    }

    [Server]
    public void OnThiefEscaped()
    {
        if (_matchEnded) return;
        _imprisonedThiefCount = Mathf.Max(0, _imprisonedThiefCount - 1);
    }

    [Server]
    public void EndMatch(string message)
    {
        _isMatchActive = false;
        if (_matchEnded) return;
        _matchEnded = true;
        CancelInvoke(nameof(ServerUpdateTimer));
        RpcShowVictory(message);
        RpcStopAllPlayers();
    }

    [ClientRpc]
    private void RpcStopAllPlayers()
    {
        var players = FindObjectsOfType<PlayerController>();
        foreach (var player in players)
        {
            player.enabled = false;
        }

        var audioSources = FindObjectsOfType<AudioSource>();
        foreach (var audio in audioSources)
        {
            audio.Stop();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    [ClientRpc]
    private void RpcUpdateTimerUI(float time)
    {
        if (timerText != null)
        {
            System.TimeSpan t = System.TimeSpan.FromSeconds(time);
            timerText.text = $"{t.Minutes:D2}:{t.Seconds:D2}";
        }
    }

    [ClientRpc]
    private void RpcShowVictory(string message)
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            victoryMessageText.text = message;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}