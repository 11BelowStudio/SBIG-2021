using Game.Ship;
using MLAPI;
using MLAPI.NetworkVariable;
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

        public override void NetworkStart()
        {
            gc = GameObject.FindWithTag("GameController").GetComponent<GameController>();
            sm = GameObject.FindObjectOfType<ShipManager>();

            gc.AddPlayer(this);
        }
        
        
    }
}