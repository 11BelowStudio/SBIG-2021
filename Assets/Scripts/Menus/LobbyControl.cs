using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Transports.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class LobbyControl : NetworkBehaviour
{
    [HideInInspector]
    public static bool isHosting;

    [FormerlySerializedAs("m_InGameSceneName")] [SerializeField]
    private string inGameSceneName = "GameScene";
    public TextMeshProUGUI LobbyText;
    private bool areAllPlayersInLobby;

    private Dictionary<ulong, bool> clientsInLobby;
    private string userLobbyStatusText;

    /// <summary>
    ///     Awake
    ///     This is one way to kick off a multiplayer session
    /// </summary>
    private void Awake()
    {
        clientsInLobby = new Dictionary<ulong, bool>();

        //We added this information to tell us if we are going to host a game or join an the game session
        if (isHosting)
        {
            SocketTasks sc = NetworkManager.Singleton.StartHost(); //Spin up the host

            bool succ = true;
            foreach (var task in sc.Tasks)
            {
                if (task.SocketError != SocketError.Success)
                {
                    succ = false;
                    break;
                }
            }
            
            if(succ)
            {
                NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
            }
            else
            {
                SceneTransitionHandler.sceneTransitionHandler.SwitchScene(SceneTransitionHandler.sceneTransitionHandler.DefaultMainMenu);
            }
            
        }
        else
        {
            NetworkManager.Singleton.StartClient(); //Spin up the client
        }

        if (NetworkManager.Singleton.IsListening)
        {
            //Always add ourselves to the list at first
            clientsInLobby.Add(NetworkManager.Singleton.LocalClientId, false);

            //If we are hosting, then handle the server side for detecting when clients have connected
            //and when their lobby scenes are finished loading.
            if (IsServer)
            {
                areAllPlayersInLobby = false;

                //Server will be notified when a client connects
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
                SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene += ClientLoadedScene;
            }

            //Update our lobby
            GenerateUserStatsForLobby();
        }

        SceneTransitionHandler.sceneTransitionHandler.SetSceneState(SceneTransitionHandler.SceneStates.Lobby);
    }

    private void ApprovalCheck(byte[] connectionData, ulong clientId, MLAPI.NetworkManager.ConnectionApprovedDelegate callback)
    {
        bool canIConnect = (clientsInLobby.Count < 4);
        callback(canIConnect, null, canIConnect, null, null);
    }

    private void OnGUI()
    {
        if (LobbyText != null)
        {
            LobbyText.text = userLobbyStatusText;
        }
    }

    /// <summary>
    ///     GenerateUserStatsForLobby
    ///     Psuedo code for setting player state
    ///     Just updating a text field, this could use a lot of "refactoring"  :)
    /// </summary>
    private void GenerateUserStatsForLobby()
    {
        userLobbyStatusText = string.Empty;
        foreach (var clientLobbyStatus in clientsInLobby)
        {
            userLobbyStatusText += "Player " + clientLobbyStatus.Key + "          ";
            if (clientLobbyStatus.Value)
            {
                userLobbyStatusText += "(Ready)\n";
            }
            else
            {
                userLobbyStatusText += "(Not Ready)\n";
            }
        }
    }

    /// <summary>
    ///     UpdateAndCheckPlayersInLobby
    ///     Checks to see if we have 4 people to start
    /// </summary>
    private void UpdateAndCheckPlayersInLobby()
    {
#if UNITY_EDITOR
        areAllPlayersInLobby = clientsInLobby.Count > 0;
#else
        areAllPlayersInLobby = clientsInLobby.Count == 4;
#endif

        foreach (var clientLobbyStatus in clientsInLobby)
        {
            SendClientReadyStatusUpdatesClientRpc(clientLobbyStatus.Key, clientLobbyStatus.Value);
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientLobbyStatus.Key))
            {

                //If some clients are still loading into the lobby scene then this is false
                areAllPlayersInLobby = false;
            }
        }

        CheckForAllPlayersReady();
    }

    /// <summary>
    ///     ClientLoadedScene
    ///     Invoked when a client has loaded this scene
    /// </summary>
    /// <param name="clientId"></param>
    private void ClientLoadedScene(ulong clientId)
    {
        if (IsServer)
        {
            if (!clientsInLobby.ContainsKey(clientId))
            {
                if (clientsInLobby.Count == 4)
                {
                    NetworkManager.Singleton.DisconnectClient(clientId);
                }
                else
                {
                    clientsInLobby.Add(clientId, false);
                    GenerateUserStatsForLobby();
                }
            }

            UpdateAndCheckPlayersInLobby();
        }
    }

    /// <summary>
    ///     OnClientConnectedCallback
    ///     Since we are entering a lobby and MLAPI NetowrkingManager is spawning the player,
    ///     the server can be configured to only listen for connected clients at this stage.
    /// </summary>
    /// <param name="clientId">client that connected</param>
    private void OnClientConnectedCallback(ulong clientId)
    {
        if (IsServer)
        {
            // forcibly disconnect a client if the lobby is full
            if (clientsInLobby.Count == 4)
            {
                NetworkManager.Singleton.DisconnectClient(clientId);
            }

            if (!clientsInLobby.ContainsKey(clientId))
            {
                clientsInLobby.Add(clientId, false);
            }
            GenerateUserStatsForLobby();

            UpdateAndCheckPlayersInLobby();
        }
    }

    /// <summary>
    ///     SendClientReadyStatusUpdatesClientRpc
    ///     Sent from the server to the client when a player's status is updated.
    ///     This also populates the connected clients' (excluding host) player state in the lobby
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="isReady"></param>
    [ClientRpc]
    private void SendClientReadyStatusUpdatesClientRpc(ulong clientId, bool isReady)
    {
        if (!IsServer)
        {
            if (!clientsInLobby.ContainsKey(clientId))
            {
                clientsInLobby.Add(clientId, isReady);
            }
            else
            {
                clientsInLobby[clientId] = isReady;
            }
            GenerateUserStatsForLobby();
        }
    }

    /// <summary>
    ///     CheckForAllPlayersReady
    ///     Checks to see if all players are ready, and if so launches the game
    /// </summary>
    private void CheckForAllPlayersReady()
    {
        if (areAllPlayersInLobby)
        {
            var allPlayersAreReady = true;
            foreach (var clientLobbyStatus in clientsInLobby)
            {
                if (!clientLobbyStatus.Value)
                {
                    //If some clients are still loading into the lobby scene then this is false
                    allPlayersAreReady = false;
                }
            }
            

            //Only if all players are ready
            if (allPlayersAreReady)
            {
                //Remove our client connected callback
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;

                //Remove our scene loaded callback
                SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene -= ClientLoadedScene;

                //Transition to the ingame scene
                SceneTransitionHandler.sceneTransitionHandler.SwitchScene(inGameSceneName);
            }
        }
    }

    /// <summary>
    ///     PlayerIsReady
    ///     Tied to the Ready button in the InvadersLobby scene
    /// </summary>
    public void PlayerIsReady()
    {
        if (IsServer)
        {
            clientsInLobby[NetworkManager.Singleton.ServerClientId] = true;
            UpdateAndCheckPlayersInLobby();
        }
        else
        {
            clientsInLobby[NetworkManager.Singleton.LocalClientId] = true;
            OnClientIsReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        GenerateUserStatsForLobby();
    }

    /// <summary>
    ///     OnClientIsReadyServerRpc
    ///     Sent to the server when the player clicks the ready button
    /// </summary>
    /// <param name="clientid">clientId that is ready</param>
    [ServerRpc(RequireOwnership = false)]
    private void OnClientIsReadyServerRpc(ulong clientid)
    {
        if (clientsInLobby.ContainsKey(clientid))
        {
            clientsInLobby[clientid] = true;
            UpdateAndCheckPlayersInLobby();
            GenerateUserStatsForLobby();
        }
    }

    
}
