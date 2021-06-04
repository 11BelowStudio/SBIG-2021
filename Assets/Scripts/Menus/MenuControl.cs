using System;
using MLAPI;
using MLAPI.Transports.UNET;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuControl : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField m_HostIpInput;
    [SerializeField]
    private TMP_InputField portInput;

    [SerializeField]
    private TextMeshProUGUI highScoreText;

    [SerializeField] private AudioSource musicPlayer;
    [SerializeField] private AudioClip fullGameTheme;
    private bool music = false;

    public void Awake()
    {
        highScoreText.SetText($"High Score:\n{PlayerPrefs.GetInt("Highscore",5)}");
        // Update the current HostNameInput with whatever we have set in the NetworkConfig as default
        var unetTransport = (UNetTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        if (unetTransport)
        {
            m_HostIpInput.text = unetTransport.ConnectAddress;
            portInput.text = unetTransport.ConnectPort.ToString();
        }

        portInput.characterValidation = TMP_InputField.CharacterValidation.Digit;
    }


    private void Update()
    {
        if (!music && Input.GetButtonDown("Jump"))
        {
            music = true;
            musicPlayer.Stop();
            musicPlayer.clip = fullGameTheme;
            musicPlayer.Play();
        }
    }


    private string m_LobbySceneName = "TheLobby";

    public void StartLocalGame()
    {
        // Update the current HostNameInput with whatever we have set in the NetworkConfig as default
        var unetTransport = (UNetTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        if (unetTransport)
        {
            m_HostIpInput.text = unetTransport.ConnectAddress;
            //portInput.text = unetTransport.ConnectPort.ToString();
            unetTransport.ServerListenPort = int.Parse(portInput.text.ToString());
        }
        LobbyControl.isHosting = true; //This is a work around to handle proper instantiation of a scene for the first time.(See LobbyControl.cs)
        SceneTransitionHandler.sceneTransitionHandler.SwitchScene(m_LobbySceneName);
    }

    public void JoinLocalGame()
    {
        if (m_HostIpInput.text != "Hostname")
        {
            var unetTransport = (UNetTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            if (unetTransport)
            {
                unetTransport.ConnectAddress = m_HostIpInput.text;
                unetTransport.ConnectPort = int.Parse(portInput.text.ToString());
            }
            LobbyControl.isHosting = false; //This is a work around to handle proper instantiation of a scene for the first time.  (See LobbyControl.cs)
            SceneTransitionHandler.sceneTransitionHandler.SwitchScene(m_LobbySceneName);
        }
    }
}
