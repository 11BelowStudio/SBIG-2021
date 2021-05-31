using Game;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace Game.Ship
{
    public class ClientShipStuff: NetworkBehaviour
    {

        public NetworkVariable<ThrustEnum> Thruster = new NetworkVariable<ThrustEnum>(new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.ServerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        });



    }
}