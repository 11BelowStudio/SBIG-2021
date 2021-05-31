using System;
using System.Collections;
using System.Collections.Generic;
using Game.Ship;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;
using UnityEngine.Assertions;
using Utilities;

namespace Game
{
    public class GameController: MonoBehaviour
    {

        private Coroutine waveSpawner;
        
        public NetworkVariable<ISet<ThrustEnum>> AllThrusts = new NetworkVariable<ISet<ThrustEnum>>(
            new NetworkVariableSettings
            {
                ReadPermission = NetworkVariablePermission.ServerOnly,
                WritePermission = NetworkVariablePermission.ServerOnly
            },
            new HashSet<ThrustEnum>());

        public NetworkVariableInt Score = new NetworkVariableInt(new NetworkVariableSettings
        {
            ReadPermission = NetworkVariablePermission.Everyone,
            WritePermission = NetworkVariablePermission.ServerOnly
        },0);

        public NetworkVariableInt Hitpoints = new NetworkVariableInt(new NetworkVariableSettings
        {
            ReadPermission = NetworkVariablePermission.Everyone,
            WritePermission = NetworkVariablePermission.ServerOnly
        });

        public readonly int DEFAULT_HITPOINTS;

        public NetworkVariable<IDictionary<ThrustEnum, ClientManager>> Clients =
            new NetworkVariable<IDictionary<ThrustEnum, ClientManager>>(
                new NetworkVariableSettings
                {
                    ReadPermission = NetworkVariablePermission.Everyone,
                    WritePermission = NetworkVariablePermission.ServerOnly
                },
                new Dictionary<ThrustEnum, ClientManager>());

        private void Start()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }
            
        }

        private void StartGame()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }

            Hitpoints.Value = DEFAULT_HITPOINTS;

        }

        public void AddPlayer(ClientManager theClient)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }

            ThrustEnum giveThis = RandomUtilities.RandomElement<ThrustEnum>(AllThrusts.Value);
            AllThrusts.Value.Remove(giveThis);
            Clients.Value.Add(giveThis, theClient);
            theClient.Thruster.Value = giveThis;

        }

        public void RemovePlayer(ClientManager theClient)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }

            ThrustEnum yeetThis = theClient.Thruster.Value;
            AllThrusts.Value.Add(yeetThis);
            Clients.Value.Remove(yeetThis);

        }

        public void ShipHit()
        {
            Assert.IsTrue(NetworkManager.Singleton.IsServer);
            Hitpoints.Value -= 1;
            if (Hitpoints.Value == 0)
            {
                GameOver();
            }
        }

        public void GameOver()
        {
            Assert.IsTrue(NetworkManager.Singleton.IsServer);
        }
        
    }
    
}