using UnityEngine;
using MLAPI;
using MLAPI.NetworkVariable;
using System.Collections;
using System.Collections.Generic;


namespace Game.Ship
{
    public class ShipManager: NetworkBehaviour
    {
        public NetworkVariable<ServerSpaceship> Spaceship = new NetworkVariable<ServerSpaceship>(new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.ServerOnly,
            ReadPermission = NetworkVariablePermission.ServerOnly
        });

        public NetworkVariableVector2 ShipXYPosition = new NetworkVariableVector2(new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.ServerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        });

        public override void NetworkStart()
        {
            
        }

        public void Update()
        {
            
        }
        
        
    }
    
}