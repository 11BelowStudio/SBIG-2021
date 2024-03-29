﻿using System;
using Game.Ship;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Extensions;
using MLAPI.NetworkVariable;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace Game.SpaceRock
{
    public class NetworkedSpaceRock: NetworkBehaviour, IAmYeetable
    {
        private NetworkObjectPool spaceRockPool;

        private GameController gameController;

        private static readonly string OBJECT_POOL_TAG = "NetworkObjectPool";

        private NetworkVariableBool Exists = new NetworkVariableBool(new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.ServerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        }, false);
        
        private NetworkVariable<Rigidbody> Rigid = new NetworkVariable<Rigidbody>(new NetworkVariableSettings
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
        });

        private Rigidbody rb;

        public float minZSpeed = 20;

        public float maxZSpeed = 75;

        private static float zRange;

        private NetworkVariableVector3 ForceToAddEveryFrame = new NetworkVariableVector3(new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.ServerOnly,
            ReadPermission = NetworkVariablePermission.ServerOnly
        });

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (IsServer)
            {
                Rigid.Value = rb;
            }
            spaceRockPool = GameObject.FindWithTag(OBJECT_POOL_TAG).GetComponent<NetworkObjectPool>();
            Assert.IsNotNull(spaceRockPool);

            zRange = maxZSpeed - minZSpeed;
        }

        

        public override void NetworkStart()
        {
            gameController = GameObject.FindWithTag("GameController").GetComponent<GameController>();
            if (IsServer)
            {
                
                float zForce = -((Random.value * zRange) + minZSpeed);

                ForceToAddEveryFrame.Value = new Vector3(0f, 0f, zForce);
                
                Rigid.Value.AddForce(ForceToAddEveryFrame.Value);
                
                //Rigid.Value.AddForce(0f,0f,-((Random.value * zRange) + minZSpeed));

                float radialDist = (Random.value + 1)/2; //between 0.5 and 1
                float polarAngle = ((Random.value * 2)-1) * 360 * Mathf.Deg2Rad; //between +180 and -180 degrees (converted to radians)
                float azimuthalAngle = ((Random.value * 2) - 1) * 360 * Mathf.Deg2Rad; //between +180 and -180 degrees (converted to radians)

                //converts the spherical coordinate stuff into cartesian

                Rigid.Value.angularVelocity = new Vector3(
                    radialDist * Mathf.Sin(polarAngle) * Mathf.Cos(azimuthalAngle),
                    radialDist * Mathf.Cos(polarAngle) * Mathf.Sin(azimuthalAngle),
                    radialDist * Mathf.Cos(polarAngle)
                );

                Position.Value = Rigid.Value.position;

                Rotation.Value = Rigid.Value.rotation;
                Exists.Value = true;
            }
            else
            {
                rb.position = Position.Value;
                rb.rotation = Rotation.Value;
            }
        }

        /// <summary>
        /// Called when this has been made from the pool
        /// </summary>
        /// <param name="startPosition">The vector3 start position for this object</param>
        public void CreatedFromPool(Vector3 startPosition)
        {
            Assert.IsTrue(IsServer);
            
            Rigid.Value.MovePosition(startPosition);
            
            float zForce = -((Random.value * zRange) + minZSpeed);

            ForceToAddEveryFrame.Value = new Vector3(0f, 0f, zForce);
                
            Rigid.Value.AddForce(ForceToAddEveryFrame.Value);

            //Rigid.Value.AddForce(0f,0f,-((Random.value * zRange) + minZSpeed));

            float radialDist = (Random.value + 1)/2; //between 0.5 and 1
            float polarAngle = ((Random.value * 2)-1) * 360 * Mathf.Deg2Rad; //between +180 and -180 degrees (converted to radians)
            float azimuthalAngle = ((Random.value * 2) - 1) * 360 * Mathf.Deg2Rad; //between +180 and -180 degrees (converted to radians)

            //converts the spherical coordinate stuff into cartesian

            Rigid.Value.angularVelocity = new Vector3(
                radialDist * Mathf.Sin(polarAngle) * Mathf.Cos(azimuthalAngle),
                radialDist * Mathf.Cos(polarAngle) * Mathf.Sin(azimuthalAngle),
                radialDist * Mathf.Cos(polarAngle)
            );

            Position.Value = Rigid.Value.position;

            Rotation.Value = Rigid.Value.rotation;
            
            Exists.Value = true;
        }
        
        public void CreatedFromPool(float startZ, float startRadius)
        {
            Assert.IsTrue(IsServer);

            Vector3 startPos = Random.insideUnitSphere * startRadius;

            //Debug.Log($"Initial start: {startPos}");
            
            startPos += new Vector3(0, 0, startZ);
            
            CreatedFromPoolAtPosition(startPos);
        }

        public void CreatedFromPoolAtPosition(Vector3 position)
        {
            Assert.IsTrue(IsServer);
            
            transform.position = position;
            
            float zForce = -((Random.value * zRange) + minZSpeed);

            ForceToAddEveryFrame.Value = new Vector3(0f, 0f, zForce);
                
            Rigid.Value.AddForce(ForceToAddEveryFrame.Value);
            
            //Rigid.Value.AddForce(0f,0f,-((Random.value * zRange) + minZSpeed));

            float radialDist = (Random.value + 1)/2; //between 0.5 and 1
            float polarAngle = ((Random.value * 2)-1) * 360 * Mathf.Deg2Rad; //between +180 and -180 degrees (converted to radians)
            float azimuthalAngle = ((Random.value * 2) - 1) * 360 * Mathf.Deg2Rad; //between +180 and -180 degrees (converted to radians)

            //converts the spherical coordinate stuff into cartesian

            Rigid.Value.angularVelocity = new Vector3(
                radialDist * Mathf.Sin(polarAngle) * Mathf.Cos(azimuthalAngle),
                radialDist * Mathf.Cos(polarAngle) * Mathf.Sin(azimuthalAngle),
                radialDist * Mathf.Cos(polarAngle)
            );

            Position.Value = position;

            Rotation.Value = Rigid.Value.rotation;
            
            Exists.Value = true;
        }

        public void FixedUpdate()
        {
            if (!Exists.Value)
            {
                return;
            }
            if (IsServer)
            {

                if (Rigid.Value.position.z <= -10)
                {
                    Yeet(true);
                }
                
                Rigid.Value.AddForce(ForceToAddEveryFrame.Value);
                
                Position.Value = Rigid.Value.position;

                Rotation.Value = Rigid.Value.rotation;
            }
            else
            {
                rb.MovePosition(Position.Value);
                rb.MoveRotation(Rotation.Value);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || !Exists.Value)
            {
                return;
            }
            if (other.tag.Equals("Player"))
            {
                //gameController.ShipHit(other.ClosestPoint(transform.position));
                gameController.ShipHit();
                //Yeet(false);
                foreach (IAmYeetable yeetThisToo in FindObjectsOfType<NetworkedSpaceRock>())
                {
                    yeetThisToo.Yeet(false);
                }
                foreach (IAmYeetable yeetThisToo in FindObjectsOfType<PointsBox>())
                {
                    yeetThisToo.Yeet(false);
                }
            }
        }

        public void Yeet(bool avoided)
        {
            Assert.IsTrue(NetworkManager.IsServer);

            if (avoided)
            {
                gameController.GainedPoint(); // score an point if this is despawning because it was avoided
            }

            Exists.Value = false;
            
            NetworkObject.Despawn();
            spaceRockPool.ReturnNetworkObject(NetworkObject);
        }

        
    }
}