﻿using System;
using System.Collections;
using System.Collections.Generic;
using Game.Ship;
using Game.SpaceRock;
using MLAPI;
using MLAPI.Extensions;
using MLAPI.NetworkVariable;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using Utilities;
using Random = UnityEngine.Random;

namespace Game
{
    public class GameController: NetworkBehaviour
    {
        

        [Header("UI Settings")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI hitpointsText;
        public TextMeshProUGUI gameOverText;
        public TextMeshProUGUI timerText;

        [Header("Spawner stuff")]
        [SerializeField]
        private NetworkObjectPool theObjectPool;
        [SerializeField]
        private GameObject SpaceRockPrefab;
        public float spawnerRadius;
        public float spawnerZPosition;
        
        private Coroutine waveSpawner;

        public int minWaveSize;
        public int maxWaveSize;
        public float minSpawnDelay;
        public float maxSpawnDelay;
        public float minWaveDelay;
        public float maxWaveDelay;
        
        
        
        [SerializeField]
        [Tooltip("Time Remaining until the game starts")]
        private float m_DelayedStartTime = 5.0f;

        private float timeRemaining;

        [SerializeField]
        private NetworkVariableFloat m_TickPeriodic = new NetworkVariableFloat(0.2f);
        
        //These help to simplify checking server vs client
        //[NSS]: This would also be a great place to add a state machine and use networked vars for this
        private bool m_ClientGameOver;
        private bool m_ClientGameStarted;
        private bool m_ClientStartCountdown;

        private NetworkVariableBool m_CountdownStarted = new NetworkVariableBool(false);

        private float m_NextTick;

        // the timer should only be synced at the beginning
        // and then let the client to update it in a predictive manner
        private NetworkVariableFloat m_ReplicatedTimeRemaining = new NetworkVariableFloat();
        private GameObject m_Saucer;
        
        private List<NetworkedSpaceRock> spaceRocks = new List<NetworkedSpaceRock>();

        public static GameController Singleton { get; private set; }

        public NetworkVariableBool hasGameStarted { get; } = new NetworkVariableBool(false);

        public NetworkVariableBool isGameOver { get; } = new NetworkVariableBool(false);

        

        public NetworkVariableInt Score = new NetworkVariableInt(new NetworkVariableSettings
        {
            ReadPermission = NetworkVariablePermission.Everyone,
            WritePermission = NetworkVariablePermission.ServerOnly
        },0);

        private int localScore = 0;

        public NetworkVariableInt Hitpoints = new NetworkVariableInt(new NetworkVariableSettings
        {
            ReadPermission = NetworkVariablePermission.Everyone,
            WritePermission = NetworkVariablePermission.ServerOnly
        });

        public int DEFAULT_HITPOINTS;

        private int localHitpoints;
        
        /*
        public NetworkVariable<ISet<ThrustEnum>> AllThrusts = new NetworkVariable<ISet<ThrustEnum>>(
            new NetworkVariableSettings
            {
                ReadPermission = NetworkVariablePermission.ServerOnly,
                WritePermission = NetworkVariablePermission.ServerOnly
            },
            new HashSet<ThrustEnum>());
            */
        private HashSet<ThrustEnum> allThrusts = new HashSet<ThrustEnum>();

        /*
        public NetworkVariable<IDictionary<ThrustEnum, ClientManager>> Clients =
            new NetworkVariable<IDictionary<ThrustEnum, ClientManager>>(
                new NetworkVariableSettings
                {
                    ReadPermission = NetworkVariablePermission.Everyone,
                    WritePermission = NetworkVariablePermission.ServerOnly
                },
                new Dictionary<ThrustEnum, ClientManager>());
                */

        private Dictionary<ThrustEnum, ClientManager> clients = new Dictionary<ThrustEnum, ClientManager>();

        /// <summary>
        ///     Awake
        ///     A good time to initialize server side values
        /// </summary>
        private void Awake()
        {
            // TODO: Improve this singleton pattern
            Singleton = this;
            OnSingletonReady?.Invoke();

            if (IsServer)
            {
                hasGameStarted.Value = false;

                //Set our time remaining locally
                timeRemaining = m_DelayedStartTime;

                //Set for server side
                m_ReplicatedTimeRemaining.Value = m_DelayedStartTime;
            }
            else
            {
                //We do a check for the client side value upon instantiating the class (should be zero)
                Debug.LogFormat("Client side we started with a timer value of {0}", m_ReplicatedTimeRemaining.Value);
            }
        }
        
        public override void NetworkStart()
        {
            if (IsClient && !IsServer)
            {
                m_ClientGameOver = false;
                m_ClientStartCountdown = false;
                m_ClientGameStarted = false;

                m_ReplicatedTimeRemaining.OnValueChanged += (oldAmount, newAmount) =>
                {
                    // See the ShouldStartCountDown method for when the server updates the value
                    if (timeRemaining == 0)
                    {
                        Debug.LogFormat("Client side our first timer update value is {0}", newAmount);
                        timeRemaining = newAmount;
                    }
                    else
                    {
                        Debug.LogFormat("Client side we got an update for a timer value of {0} when we shouldn't", m_ReplicatedTimeRemaining.Value);
                    }
                };

                m_CountdownStarted.OnValueChanged += (oldValue, newValue) =>
                {
                    m_ClientStartCountdown = newValue;
                    Debug.LogFormat("Client side we were notified the start count down state was {0}", newValue);
                };

                hasGameStarted.OnValueChanged += (oldValue, newValue) =>
                {
                    m_ClientGameStarted = newValue;
                    timerText.gameObject.SetActive(!m_ClientGameStarted);
                    Debug.LogFormat("Client side we were notified the game started state was {0}", newValue);
                };

                isGameOver.OnValueChanged += (oldValue, newValue) =>
                {
                    m_ClientGameOver = newValue;
                    Debug.LogFormat("Client side we were notified the game over state was {0}", newValue);
                    if (newValue == true)
                    {
                        GameOver();
                    }
                };

                Score.OnValueChanged += (oldValue, newValue) =>
                {
                    localScore = newValue;
                    scoreText.SetText($"Score:\n{newValue}");
                };

                Hitpoints.OnValueChanged += (oldValue, newValue) =>
                {
                    localHitpoints = newValue;
                    hitpointsText.SetText($"Hitpoints:\n{newValue}");
                    if (newValue == 0)
                    {
                        isGameOver.Value = true;
                    }
                };
            }

            //Both client and host/server will set the scene state to "ingame" which places the PlayerControl into the SceneTransitionHandler.SceneStates.INGAME
            //and in turn makes the players visible and allows for the players to be controlled.
            SceneTransitionHandler.sceneTransitionHandler.SetSceneState(SceneTransitionHandler.SceneStates.Ingame);

            base.NetworkStart();
        }
        internal static event Action OnSingletonReady;
        
        /// <summary>
        ///     ShouldStartCountDown
        ///     Determines when the countdown should start
        /// </summary>
        /// <returns>true or false</returns>
        private bool ShouldStartCountDown()
        {
            //If the game has started, then don't both with the rest of the count down checks.
            if (HasGameStarted()) return false;
            if (IsServer)
            {
                m_CountdownStarted.Value = SceneTransitionHandler.sceneTransitionHandler.AllClientsAreLoaded();

                //While we are counting down, continually set the m_ReplicatedTimeRemaining.Value (client should only receive the update once)
                if (m_CountdownStarted.Value && m_ReplicatedTimeRemaining.Settings.SendTickrate != -1)
                {
                    //Now we can specify that we only want this to be sent once
                    m_ReplicatedTimeRemaining.Settings.SendTickrate = -1;

                    //Now set the value for our one time m_ReplicatedTimeRemaining networked var for clients to get updated once
                    m_ReplicatedTimeRemaining.Value = m_DelayedStartTime;
                }

                return m_CountdownStarted.Value;
            }

            return m_ClientStartCountdown;
        }

        /// <summary>
        ///     IsCurrentGameOver
        ///     Returns whether the game is over or not
        /// </summary>
        /// <returns>true or false</returns>
        private bool IsCurrentGameOver()
        {
            if (IsServer)
            {
                return isGameOver.Value;
            }
            return m_ClientGameOver;
        }
        
        /// <summary>
        ///     HasGameStarted
        ///     Determine whether the game has started or not
        /// </summary>
        /// <returns>true or false</returns>
        private bool HasGameStarted()
        {
            if (IsServer)
            {
                return hasGameStarted.Value;
            }

            return m_ClientGameStarted;
        }
        
        /// <summary>
        ///     Client side we try to predictively update the gameTimer
        ///     as there shouldn't be a need to receive another update from the server
        ///     We only got the right m_TimeRemaining value when we started so it will be enough
        /// </summary>
        /// <returns> True when m_HasGameStared is set </returns>
        private void UpdateGameTimer()
        {
            if (!ShouldStartCountDown()) return;
            if (!HasGameStarted() && timeRemaining > 0.0f)
            {
                timeRemaining -= Time.deltaTime;

                if (IsServer) // Only the server should be updating this
                {
                    if (timeRemaining <= 0.0f)
                    {
                        timeRemaining = 0.0f;
                        hasGameStarted.Value = true;
                        OnGameStarted();
                    }

                    m_ReplicatedTimeRemaining.Value = timeRemaining;
                }

                if (timeRemaining > 0.1f)
                {
                    timerText.SetText("{0}", Mathf.FloorToInt(timeRemaining));
                }
            }
        }

        /// <summary>
        ///     OnGameStarted
        ///     Only invoked by the server, this hides the timer text starts the game
        /// </summary>
        private void OnGameStarted()
        {
            Assert.IsTrue(IsServer);
            timerText.gameObject.SetActive(false);
            
            Hitpoints.Value = DEFAULT_HITPOINTS;

            waveSpawner = StartCoroutine(WaveSpawnerCoroutine().GetEnumerator());


        }

        /// <summary>
        /// This is the coroutine for the wave spawning stuff
        /// </summary>
        /// <returns></returns>
        private IEnumerable WaveSpawnerCoroutine()
        {
            while(IsCurrentGameOver())
            {
                int currentWaveSize = Random.Range(minWaveSize, maxWaveSize);
                for (int i = 0; i < currentWaveSize; i++)
                {
                    GameObject theNewSpaceRock = theObjectPool.GetNetworkObject(SpaceRockPrefab);

                    Vector2 randomPos2D = Random.insideUnitCircle * spawnerRadius;

                    Vector3 randomPos3D = new Vector3(randomPos2D.x, randomPos2D.y, spawnerZPosition);

                    theNewSpaceRock.GetComponent<NetworkedSpaceRock>().CreatedFromPool(randomPos3D);
                    
                    theNewSpaceRock.GetComponent<NetworkObject>().Spawn(null, true);

                    yield return new WaitForSeconds(Random.Range(minSpawnDelay, maxSpawnDelay));
                }

                maxWaveSize += 1;

                yield return new WaitForSeconds(Random.Range(minWaveDelay, maxWaveDelay));
            }
            yield break;
        }

        

        public void AddPlayer(ClientManager theClient)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }

            ThrustEnum giveThis = RandomUtilities.RandomElement<ThrustEnum>(allThrusts);
            allThrusts.Remove(giveThis);
            clients.Add(giveThis, theClient);
            theClient.Thruster.Value = giveThis;

        }

        public void RemovePlayer(ClientManager theClient)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }

            ThrustEnum yeetThis = theClient.Thruster.Value;
            allThrusts.Add(yeetThis);
            clients.Remove(yeetThis);

        }

        /// <summary>
        /// To be called when a point is scored.
        /// Points are scored when a space rock has been avoided (despawns naturally)
        /// </summary>
        public void GainedPoint()
        {
            if (IsServer) // only the server can officially declare a point scored.
            {
                if (!isGameOver.Value) // if the game isn't over yet
                {
                    Score.Value += 1; // score 1 point!
                }
            }
        }

        public void ShipHit()
        {
            Assert.IsTrue(NetworkManager.Singleton.IsServer);
            Hitpoints.Value -= 1;
        }

        public void GameOver()
        {
            Assert.IsTrue(NetworkManager.Singleton.IsServer);
            hitpointsText.SetText("DED");
            gameOverText.gameObject.SetActive(true);
            
            
        }
        
        public void DisplayGameOverText(string message)
        {

        }
        
    }
    
}