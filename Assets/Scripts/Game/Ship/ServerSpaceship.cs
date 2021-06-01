using System;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace Game.Ship
{
    public class ServerSpaceship: NetworkBehaviour
    { 
        
        [SerializeField] private float maxSpeed = 10;
        [SerializeField] private float maxTilt = 25;

        [SerializeField] private float xyBounds = 5;

        [SerializeField] private float speed = 100;

        public HashSet<ThrustEnum> currentThrusts = new HashSet<ThrustEnum>();

        //[SerializeField] private GameObject prefabPlayerShot;

        //[SerializeField] private GameObject playerShotSpawner;

        //[SerializeField] private float shotDelaySecs;

        //private float shotTimer;

        public NetworkVariable<Rigidbody> Rigid = new NetworkVariable<Rigidbody>(new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.ServerOnly,
            ReadPermission = NetworkVariablePermission.ServerOnly
        });
        
        public NetworkVariableVector3 Position = new NetworkVariableVector3(new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.ServerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        });

        public NetworkVariableQuaternion Rotation = new NetworkVariableQuaternion(new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.ServerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        }, Quaternion.LookRotation(new Vector3(0,0,1)).normalized );

        
        private Vector2 currentThrust;

        private Rigidbody rb;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            if (IsServer)
            {
                Rigid.Value = rb;
                Position.Value = rb.position;
                Rotation.Value = rb.rotation;
            }
        }

        // Start is called before the first frame update
        public override void NetworkStart()
        {
            currentThrust = Vector2.zero;
        }

        [ServerRpc]
        public void ApplyThrustToShipServerRPC(ThrustEnum t)
        {
            currentThrusts.Add(t);
        }
        
        [ServerRpc]
        public void RemoveThrustFromShipServerRPC(ThrustEnum t)
        {
            currentThrusts.Remove(t);
        }

        
        private void Update()
        {

            if (!IsServer)
            {
                return;
            }
            
            Vector2 addThrust = Vector2.zero;

            foreach (ThrustEnum t in currentThrusts)
            {
                addThrust += ThrustEnumUtilities.ThrustEnumToVector2(t);
            }

            


            if (addThrust.magnitude > 1)
            {
                addThrust.Normalize();
            }

            currentThrust = addThrust;

        }

        private void FixedUpdate()
        {
            if (IsServer)
            {

                Vector3 movement = new Vector3(currentThrust.x, currentThrust.y); //new Vector3(xInput, 0.0f);


                Vector3 currentVelocity = Rigid.Value.velocity;
                //clamping velocity of shipBody if it's too fast
                if (currentVelocity.magnitude > maxSpeed)
                {
                    Rigid.Value.velocity = ((currentVelocity.normalized) * maxSpeed);
                }


                Vector3 currentPos = Rigid.Value.position;

                Vector2 xyPos = new Vector2(currentPos.x, currentPos.y);

                float tempXBounds = xyBounds;

                float tempYBounds = xyBounds;

                if (xyPos.magnitude > xyBounds)
                {
                    Vector2 bounded = xyPos.normalized * xyBounds;
                    tempXBounds = Mathf.Abs(bounded.x);
                    tempYBounds = Mathf.Abs(bounded.y);
                }

                bool needToClampX = (currentPos.x > tempXBounds || currentPos.x < -tempXBounds);

                bool needToClampY = (currentPos.y > tempYBounds || currentPos.y < -tempYBounds);

                if (needToClampX || needToClampY)
                {
                    float newX = currentPos.x;
                    float newY = currentPos.y;

                    Vector3 angVel = Rigid.Value.angularVelocity;
                    float newAngVelX = angVel.x;
                    float newAngVelY = angVel.y;

                    if (needToClampX)
                    {
                        newX = Mathf.Clamp(newX, -tempXBounds, tempXBounds);
                        newAngVelX = 0;
                    }

                    if (needToClampY)
                    {
                        newY = Mathf.Clamp(newY, -tempYBounds, tempYBounds);
                        newAngVelY = 0;
                    }

                    Rigid.Value.position = new Vector3(newX, newY, currentPos.z);
                    Rigid.Value.angularVelocity = new Vector3(newAngVelY, 0, newAngVelX);
                }



                //actually adding the force to move it
                Rigid.Value.AddForce(movement * (speed * Time.fixedDeltaTime));

                //tilting the ship
                float sideTilt = Mathf.Clamp(Rigid.Value.velocity.x / (maxSpeed / 2), -1, 1);

                float verticalTilt = Mathf.Clamp(Rigid.Value.velocity.y / (maxSpeed / 2), -1, 1);

                Rigid.Value.rotation = Quaternion.Euler(verticalTilt * -maxTilt, 0f, sideTilt * -maxTilt);

                Position.Value = Rigid.Value.position;

                Rotation.Value = Rigid.Value.rotation;

            }
            else
            {
                rb.MovePosition(Position.Value);
                rb.MoveRotation(Rotation.Value.normalized);
            }

        }

    }
}