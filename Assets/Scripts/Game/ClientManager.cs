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

        private GameController gc;

        private ShipManager sm;

        private SceneTransitionHandler.SceneStates currentSceneState;
        
        private ClientRpcParams ownerRpcParams;

        private bool hasGameStarted;
        
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
                GameController.Singleton.isGameOver.OnValueChanged -= OnGameStartedChanged;
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
                gc = GameController.Singleton;
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
            GameController.Singleton.hasGameStarted.OnValueChanged += OnGameStartedChanged;
            GameController.Singleton.isGameOver.OnValueChanged += OnGameStartedChanged;
        }



        private void OnGameStartedChanged(bool previousValue, bool newValue)
        {
            hasGameStarted = newValue;
        }
        
        public void Update()
        {
            if (IsServer)
            {
                UpdateServer();    
            }

            if (IsClient)
            {
                UpdateClient();
            }
        }

        private void UpdateServer()
        {
            
        }

        private void UpdateClient()
        {
            if (!IsLocalPlayer)
            {
                return;
            }

            if (hasGameStarted && (Input.anyKey || Input.GetButton("Jump")))
            {
                RequestThrust();
            }
        }

        private void RequestThrust()
        {
            sm.ThrustRequestServerRpc(Thruster.Value);
        }

        
        
    }
}