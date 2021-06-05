using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Ship;
using Game.SpaceRock;
using MLAPI;
using MLAPI.Extensions;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.NetworkVariable.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
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
        public TextMeshProUGUI newHighScoreText;
        public TextMeshProUGUI timerText;

        [Header("Spawner stuff")]
        [SerializeField]
        public NetworkObjectPool theObjectPool;
        [SerializeField]
        private GameObject SpaceRockPrefab;
        [SerializeField]
        private float spawnerRadius;
        [SerializeField]
        private float spawnerZPosition;
        
        private Coroutine waveSpawner;
        
        [SerializeField]
        private int minWaveSize;
        [SerializeField]
        private int maxWaveSize;
        [SerializeField]
        private float minSpawnDelay;
        [SerializeField]
        private float maxSpawnDelay;
        [SerializeField]
        private float minWaveDelay;
        [SerializeField]
        private float maxWaveDelay;

        [Header("Points box stuff")]
        [SerializeField]
        private GameObject pointsBoxPrefab;

        private Coroutine boxSpawner;

        [SerializeField]
        private float minPointsBoxDelay;
        [SerializeField]
        private float maxPointsBoxDelay;

        [SerializeField]
        private AudioSource pointsScoredAudioSource;



        [Header("Other stuff")]
        [SerializeField]
        private GameObject explosionPrefab;

        [FormerlySerializedAs("m_DelayedStartTime")]
        [SerializeField]
        [Tooltip("Time Remaining until the game starts")]
        private float delayedStartTime = 5.0f;

        private float timeRemaining;

        [FormerlySerializedAs("m_TickPeriodic")] [SerializeField]
        private NetworkVariableFloat tickPeriodic = new NetworkVariableFloat(0.2f);
        
        //These help to simplify checking server vs client
        //[NSS]: This would also be a great place to add a state machine and use networked vars for this
        private bool clientGameOver;
        private bool clientGameStarted;
        private bool clientStartCountdown;

        private NetworkVariableBool countdownStarted = new NetworkVariableBool(false);

        private float nextTick;

        // the timer should only be synced at the beginning
        // and then let the client to update it in a predictive manner
        private NetworkVariableFloat replicatedTimeRemaining = new NetworkVariableFloat();

        //private List<NetworkedSpaceRock> spaceRocks = new List<NetworkedSpaceRock>();

        public static GameController Singleton { get; private set; }

        public NetworkVariableBool hasGameStarted { get; } = new NetworkVariableBool(false);

        public NetworkVariableBool isGameOver { get; } = new NetworkVariableBool(false);

        public AudioSource gameMusicAudioSource;

        private bool isInGracePeriod = false;

        [SerializeField]
        private float gracePeriodLength = 3f;

        

        public NetworkVariableFloat Score = new NetworkVariableFloat(new NetworkVariableSettings
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
        

        private List<ThrustEnum> allThrusts = new List<ThrustEnum>((ThrustEnum[]) Enum.GetValues(typeof(ThrustEnum)));

        
        public NetworkDictionary<ThrustEnum, ClientManager> Clients =
            new NetworkDictionary<ThrustEnum, ClientManager>(
                new NetworkVariableSettings
                {
                    ReadPermission = NetworkVariablePermission.Everyone,
                    WritePermission = NetworkVariablePermission.ServerOnly
                },
                new Dictionary<ThrustEnum, ClientManager>());

        private ServerSpaceship theSpaceship;

        /// <summary>
        ///     Awake
        ///     A good time to initialize server side values
        /// </summary>
        private void Awake()
        {
            // TODO: Improve this singleton pattern
            Singleton = this;
            OnSingletonReady?.Invoke();

            //updateTheHitpoints = true;
            //updateTheScore = true;
    
            if (IsServer)
            {
                hasGameStarted.Value = false;

                Score.Value = 0;
                Hitpoints.Value = DEFAULT_HITPOINTS;

                //Set our time remaining locally
                timeRemaining = delayedStartTime;

                //Set for server side
                replicatedTimeRemaining.Value = delayedStartTime;

                theSpaceship = FindObjectOfType<ServerSpaceship>();

                IList<ClientManager> allClients = FindObjectsOfType<ClientManager>();
                
                RandomUtilities.Shuffle(allClients);
                foreach (var c in allClients)
                {
                    AddPlayer(c);
                }

            }
            else
            {
                //We do a check for the client side value upon instantiating the class (should be zero)
                Debug.LogFormat("Client side we started with a timer value of {0}", replicatedTimeRemaining.Value);
            }

            localHitpoints = DEFAULT_HITPOINTS;
            scoreText.SetText($"Score:\n0");
            hitpointsText.SetText($"Hitpoints:\n{localHitpoints}");
            gameMusicAudioSource.Play();
            
        }
        
        public override void NetworkStart()
        {
            if (IsClient)
            {
                clientGameOver = false;
                clientStartCountdown = false;
                clientGameStarted = false;

                replicatedTimeRemaining.OnValueChanged += (oldAmount, newAmount) =>
                {
                    // See the ShouldStartCountDown method for when the server updates the value
                    if (timeRemaining == 0)
                    {
                        Debug.LogFormat("Client side our first timer update value is {0}", newAmount);
                        timeRemaining = newAmount;
                    }
                    else
                    {
                        Debug.LogFormat("Client side we got an update for a timer value of {0} when we shouldn't", replicatedTimeRemaining.Value);
                    }
                };

                countdownStarted.OnValueChanged += (oldValue, newValue) =>
                {
                    clientStartCountdown = newValue;
                    Debug.LogFormat("Client side we were notified the start count down state was {0}", newValue);
                    if (clientStartCountdown)
                    {
                        gameMusicAudioSource.Play();
                    }
                };

                hasGameStarted.OnValueChanged += (oldValue, newValue) =>
                {
                    clientGameStarted = newValue;
                    timerText.gameObject.SetActive(!clientGameStarted);
                    Debug.LogFormat("Client side we were notified the game started state was {0}", newValue);
                };

                isGameOver.OnValueChanged += (oldValue, newValue) =>
                {
                    clientGameOver = newValue;
                    Debug.LogFormat("Client side we were notified the game over state was {0}", newValue);
                    if (newValue == true)
                    {
                        GameOver();
                    }
                };

                Score.OnValueChanged += (oldValue, newValue) =>
                {
                    localScore = Mathf.FloorToInt(newValue);
                    scoreText.SetText($"Score:\n{localScore}");
                };

                Hitpoints.OnValueChanged += (oldValue, newValue) =>
                {
                    localHitpoints = newValue;
                    hitpointsText.SetText($"Hitpoints:\n{localHitpoints}");
                    if (newValue == 0)
                    {
                        if (IsServer)
                        {
                            isGameOver.Value = true;
                        }
                    }
                    else if (newValue > oldValue && clientGameStarted)
                    {
                        pointsScoredAudioSource.Play();
                    }
                };
            }

            //Both client and host/server will set the scene state to "ingame" which places the PlayerControl into the SceneTransitionHandler.SceneStates.INGAME
            //and in turn makes the players visible and allows for the players to be controlled.
            SceneTransitionHandler.sceneTransitionHandler.SetSceneState(SceneTransitionHandler.SceneStates.Ingame);

            base.NetworkStart();
        }
        internal static event Action OnSingletonReady;

        internal static event Action PleaseGoAwayThanks;
        
        
        /// <summary>
        ///     Update
        ///     MonoBehaviour Update method
        /// </summary>
        private void Update()
        {
            //Is the game over?
            if (IsCurrentGameOver())
            {
                return;
            }

            //Update game timer (if the game hasn't started)
            if (clientGameStarted)
            {
                hitpointsText.SetText($"Hitpoints:\n{localHitpoints}");
                scoreText.SetText($"Score:\n{localScore}");
            }
            else
            {
                UpdateGameTimer();
            }

            //hitpointsText.SetText($"Hitpoints:\n{Hitpoints.Value}");
            //scoreText.SetText($"Score:\n{Score.Value}");

            /*
            if (updateTheHitpoints)
            {
                hitpointsText.SetText($"Hitpoints:\n{localHitpoints}");
                updateTheHitpoints = false;
            }

            if (updateTheScore)
            {
                scoreText.SetText($"Score:\n{localScore}");
                updateTheScore = false;
            }
            */

            //If we are a connected client, then don't update the enemies (server side only)
            if (!IsServer)
            {
                return;
            }

            //If we are the server and the game has started, then update the enemies
            //if (HasGameStarted()) UpdateEnemies();
        }
        
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
                countdownStarted.Value = SceneTransitionHandler.sceneTransitionHandler.AllClientsAreLoaded();

                //While we are counting down, continually set the m_ReplicatedTimeRemaining.Value (client should only receive the update once)
                if (countdownStarted.Value && replicatedTimeRemaining.Settings.SendTickrate != -1)
                {
                    //Now we can specify that we only want this to be sent once
                    replicatedTimeRemaining.Settings.SendTickrate = -1;

                    //Now set the value for our one time m_ReplicatedTimeRemaining networked var for clients to get updated once
                    replicatedTimeRemaining.Value = delayedStartTime;
                }

                return countdownStarted.Value;
            }

            return clientStartCountdown;
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
            return clientGameOver;
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

            return clientGameStarted;
        }
        
        /// <summary>
        ///     Client side we try to predictively update the gameTimer
        ///     as there shouldn't be a need to receive another update from the server
        ///     We only got the right m_TimeRemaining value when we started so it will be enough
        /// </summary>
        /// <returns> True when m_HasGameStared is set </returns>
        private void UpdateGameTimer()
        {
            if (!ShouldStartCountDown())
            {
                return;
            }
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

                    replicatedTimeRemaining.Value = timeRemaining;
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
            hasGameStarted.Value = true;
            timerText.gameObject.SetActive(false);
            
            Hitpoints.Value = DEFAULT_HITPOINTS;

            waveSpawner = StartCoroutine(WaveSpawnerCoroutine().GetEnumerator());

            boxSpawner = StartCoroutine(PointsBoxSpawnerCoroutine().GetEnumerator());

        }

        /// <summary>
        /// This is the coroutine for the wave spawning stuff
        /// </summary>
        /// <returns></returns>
        private IEnumerable WaveSpawnerCoroutine()
        {
            while(!IsCurrentGameOver())
            {
                int currentWaveSize = Random.Range(minWaveSize, maxWaveSize);
                //Debug.Log(currentWaveSize);
                for (int i = 0; i < currentWaveSize; i++)
                {

                    yield return new WaitWhile(() => isInGracePeriod);
                    
                    GameObject theNewSpaceRock = theObjectPool.GetNetworkObject(SpaceRockPrefab);


                    if (Random.value < 0.1f) // 10% chance of anti-camper space rocks appearing
                    {
                        Debug.Log("RIGHT THAT'S IT");
                        Vector3 spaceshipPos = theSpaceship.transform.position;
                        theNewSpaceRock.GetComponent<NetworkedSpaceRock>().CreatedFromPoolAtPosition(
                            new Vector3(spaceshipPos.x, spaceshipPos.y, spawnerZPosition)
                        );
                    }
                    else // space rock at a random position who cares
                    {
                        theNewSpaceRock.GetComponent<NetworkedSpaceRock>().CreatedFromPool(spawnerZPosition, spawnerRadius);
                    }

                    theNewSpaceRock.GetComponent<NetworkObject>().Spawn(null, true);

                    yield return new WaitForSeconds(Random.Range(minSpawnDelay, maxSpawnDelay));
                }

                maxWaveSize += 1;

                yield return new WaitForSeconds(Random.Range(minWaveDelay, maxWaveDelay));
            }
            yield break;
        }
        
        /// <summary>
        /// This is the coroutine for the points box spawning stuff
        /// </summary>
        /// <returns></returns>
        private IEnumerable PointsBoxSpawnerCoroutine()
        {
            while(!IsCurrentGameOver())
            {
               
                GameObject theNewPointsBox = theObjectPool.GetNetworkObject(pointsBoxPrefab);

                   
                theNewPointsBox.GetComponent<PointsBox>().CreatedFromPool(spawnerZPosition, spawnerRadius);
                    
                theNewPointsBox.GetComponent<NetworkObject>().Spawn(null, true);

                yield return new WaitForSeconds(Random.Range(minPointsBoxDelay, maxPointsBoxDelay));
            }
            yield break;
        }

        

        public void AddPlayer(ClientManager theClient)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }


            //RandomUtilities.Shuffle(allThrusts); // I do a little trolling.

            //IList<ThrustEnum> thrustList =
            //    RandomUtilities.ShuffleCopy(new List<ThrustEnum>((ThrustEnum[]) Enum.GetValues(typeof(ThrustEnum))));
            
            
            foreach (ThrustEnum t in Enum.GetValues(typeof(ThrustEnum)))//thrustList) //allThrusts) //Enum.GetValues(typeof(ThrustEnum))
            {
                if (!Clients.ContainsKey(t))
                {
                    Clients.Add(t, theClient);
                    theClient.Thruster.Value = t;
                    break;
                }
            }
            
            

            //ThrustEnum giveThis = RandomUtilities.RandomElement<ThrustEnum>(allThrusts);
            //allThrusts.Remove(giveThis);
            //Clients.Add(thisOne, theClient);
            //Debug.Log(thisOne.ToString());
            //theClient.Thruster.Value = thisOne;

        }

        public void RemovePlayer(ClientManager theClient)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                RemovePlayerServerRPC(theClient.Thruster.Value);
                return;
            }

            ThrustEnum yeetThis = theClient.Thruster.Value;
            allThrusts.Add(yeetThis);
            Clients.Remove(yeetThis);

        }

        [ServerRpc]
        private void RemovePlayerServerRPC(ThrustEnum yeetThis)
        {
            allThrusts.Add(yeetThis);
            Clients.Remove(yeetThis);
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
                    Score.Value += Random.Range(0.06125f, 0.25f); // score anywhere between 1/16 and 1/4 point!
                }
            }
        }

        public void HitPointsBox()
        {
            Assert.IsTrue(IsServer);

            if (isGameOver.Value)
            {
                return;
            }
            
            Score.Value += 3;
            if (Hitpoints.Value < DEFAULT_HITPOINTS)
            {
                Hitpoints.Value += 1;
            }

        }

        /// <summary>
        /// Basically gives a grace period after getting hit by a space rock
        /// </summary>
        /// <returns></returns>
        private IEnumerable gracePeriodCoroutine()
        {
            if (!isInGracePeriod)
            {
                isInGracePeriod = true;
                yield return new WaitForSeconds(gracePeriodLength);
                isInGracePeriod = false;
            }

            yield break;
        }

        public void ShipHit()
        {
            Assert.IsTrue(NetworkManager.Singleton.IsServer);
            if (isGameOver.Value || isInGracePeriod)
            {
                return;
            }
            Hitpoints.Value -= 1;
            theSpaceship.GoBackToTheMiddle();

            StartCoroutine(gracePeriodCoroutine().GetEnumerator());

            foreach (var client in Clients.Values)
            {
                client.gc.SpawnExplosionClientRPC(theSpaceship.explosionGoesHere.transform.position);
            }
            
        }

        [ClientRpc]
        public void SpawnExplosionClientRPC(Vector3 explosionPosition)
        {
            Instantiate(explosionPrefab, explosionPosition, transform.rotation);
        }

        public void GameOver()
        {
            hitpointsText.SetText("DED");
            gameOverText.gameObject.SetActive(true);
            gameMusicAudioSource.Stop();

            float lastHighScore = PlayerPrefs.GetInt("Highscore", 0);
            if (localScore > lastHighScore)
            {
                PlayerPrefs.SetInt("Highscore",localScore);
            }


        }

        public void DisplayGameOverText(string message)
        {

        }
        
        public void ExitGame()
        {
            if (IsServer)
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

            if (IsClient)
            {
                NetworkManager.Singleton.StopClient();
            }

            SceneTransitionHandler.sceneTransitionHandler.ExitAndLoadStartMenu();
        }
        
        
    }
    
}