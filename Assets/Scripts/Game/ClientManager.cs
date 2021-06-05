using System.Collections.Generic;
using Game.Ship;
using MLAPI;
using MLAPI.NetworkVariable;
using MLAPI.Messaging;
using UnityEngine;
using UnityEngine.Assertions;

namespace Game
{
    public class ClientManager: NetworkBehaviour
    {
        public NetworkVariable<ThrustEnum> Thruster = new NetworkVariable<ThrustEnum>(new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.ServerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        });

        public GameController gc;

        private ShipManager sm;

        private SceneTransitionHandler.SceneStates currentSceneState;
        
        private ClientRpcParams ownerRpcParams;

        private bool hasGameStarted;

        private bool isGameOver;
        
        public void Start()
        {
            DontDestroyOnLoad(gameObject);
        }
        
        
        protected void OnDestroy()
        {
            if (IsClient)
            {
                //m_Lives.OnValueChanged -= OnLivesChanged;
                //m_Lives.OnValueChanged -= OnScoreChanged;
            }

            if (GameController.Singleton)
            {
                //gc.RemovePlayer(this);
                GameController.Singleton.isGameOver.OnValueChanged -= OnGameOverChanged;
                GameController.Singleton.hasGameStarted.OnValueChanged -= OnGameStartedChanged;
            }
        }

        private void SceneTransitionHandler_clientLoadedScene(ulong clientId)
        {
            SceneStateChangedClientRpc(currentSceneState);
        }

        [ClientRpc]
        private void SceneStateChangedClientRpc(SceneTransitionHandler.SceneStates state)
        {
            if (!IsServer) SceneTransitionHandler.sceneTransitionHandler.SetSceneState(state);
        }

        private void SceneTransitionHandler_sceneStateChanged(SceneTransitionHandler.SceneStates newState)
        {
            currentSceneState = newState;
            if (currentSceneState == SceneTransitionHandler.SceneStates.Ingame)
            {
                //gc = GameController.Singleton;
                sm = FindObjectOfType<ShipManager>();
                //if (m_PlayerVisual != null) m_PlayerVisual.material.color = Color.green;
            }
            else
            {
                //if (m_PlayerVisual != null) m_PlayerVisual.material.color = Color.black;
            }
        }

        public override void NetworkStart()
        {
            base.NetworkStart();

            // Bind to OnValueChanged to display in log the remaining lives of this player
            // And to update InvadersGame singleton client-side
            //m_Lives.OnValueChanged += OnLivesChanged;
            //m_Score.OnValueChanged += OnScoreChanged;

            if (IsServer)
            {
                ownerRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
                };
            }
            else
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += ForciblyDisconnected;
            }

            if (!GameController.Singleton)
            {
                GameController.OnSingletonReady += SubscribeToDelegatesAndUpdateValues;
            }
            else
            {
                SubscribeToDelegatesAndUpdateValues();
            }

            if (IsServer)
            {
                SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene += SceneTransitionHandler_clientLoadedScene;
            }

            SceneTransitionHandler.sceneTransitionHandler.OnSceneStateChanged += SceneTransitionHandler_sceneStateChanged;
        }

        private void SubscribeToDelegatesAndUpdateValues()
        {
            gc = GameController.Singleton;
            //gc.AddPlayer(this);
            gc.hasGameStarted.OnValueChanged += OnGameStartedChanged;
            gc.isGameOver.OnValueChanged += OnGameOverChanged;
            GameController.PleaseGoAwayThanks += ForceQuit;
        }



        private void OnGameStartedChanged(bool previousValue, bool newValue)
        {
            hasGameStarted = newValue;
        }
        
        private void OnGameOverChanged(bool previousValue, bool newValue)
        {
            isGameOver = newValue;
        }

        public void Update()
        {

            switch (currentSceneState)
            {
                case SceneTransitionHandler.SceneStates.Ingame:
                {
                    InGameUpdate();
                    break;
                }
            }
        }
        private void InGameUpdate()
        {

            if (IsClient)
            {
                UpdateClient();
            }
        }

        private void UpdateClient()
        {

            if (!IsLocalPlayer || !IsOwner || !hasGameStarted) return;

            if (Input.GetButtonDown("Jump"))
            {
                if (isGameOver)
                {
                    Quit();
                    SceneTransitionHandler.sceneTransitionHandler.ExitAndLoadStartMenu();
                }
                else if (hasGameStarted)
                {
                    RequestThrustServerRPC();
                }
            }
            else if (Input.GetButtonUp("Jump"))
            {
                RequestStopThrustServerRPC();
            }
            
            

        }

        private void Quit()
        {
            GameController.PleaseGoAwayThanks -= ForceQuit;
            gc.ExitGame();
            SceneTransitionHandler.sceneTransitionHandler.ExitAndLoadStartMenu();
        }

        private void ForceQuit()
        {
            if (!IsServer)
            {
                Quit();
            }
        }

        [ServerRpc]
        private void RequestThrustServerRPC()
        {
            //Debug.Log($"Requesting {Thruster.Value.ToString()} thrust!");
            sm.ThrustRequestServerRpc(Thruster.Value);
        }
        
        [ServerRpc]
        private void RequestStopThrustServerRPC()
        {
            if (!isGameOver)
            {
                sm.ThrustStopRequestServerRPC(Thruster.Value);
            }
        }
        

        private void ForciblyDisconnected(ulong c)
        {
            if (IsServer)
            {
                return;
            }
            else if (c == OwnerClientId)
            {
                SceneTransitionHandler.sceneTransitionHandler.ExitAndLoadStartMenu();
            }
        }
        
        private void OnApplicationQuit()
        {

            if (!IsServer)
            {
                NetworkManager.Singleton.StopClient();
            }
            else
            {
                List<ulong> clients = new List<ulong>(NetworkManager.Singleton.ConnectedClients.Keys);
                foreach (var c in clients)
                {
                    if (c != OwnerClientId)
                    {
                        NetworkManager.Singleton.DisconnectClient(c);
                    }
                }
                NetworkManager.Singleton.StopServer();
            }
            NetworkManager.Shutdown();

        }



    }
}