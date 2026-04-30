using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class PlayerLobbyVisuals : NetworkBehaviour
{
    void Start()
    {
        UpdateState();
    }

    public override void OnStartClient()
    {
        UpdateState();
    }

    void UpdateState()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "Lobby")
        {
            SetPlayerActive(false);
        }
        else if (sceneName == "GameScene")
        {
            SetPlayerActive(true);
        }
    }

    void SetPlayerActive(bool state)
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = state;

        if (isLocalPlayer)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cam.enabled = state;

            AudioListener listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = state;
            
            if (TryGetComponent(out PlayerController pc)) pc.enabled = state;
        }
    }
}