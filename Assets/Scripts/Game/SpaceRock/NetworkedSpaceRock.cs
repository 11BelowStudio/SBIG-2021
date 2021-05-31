﻿using System;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Extensions;
using MLAPI.NetworkVariable;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace Game.SpaceRock
{
    public class NetworkedSpaceRock: NetworkBehaviour
    {
        private NetworkObjectPool spaceRockPool;

        private GameController gameController;

        private static readonly string OBJECT_POOL_TAG = "GameController";
        
        private NetworkVariable<Rigidbody> Rigid = new NetworkVariable<Rigidbody>(new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.ServerOnly,
            ReadPermission = NetworkVariablePermission.ServerOnly
        });
        
        private NetworkVariableVector3 Position = new NetworkVariableVector3(new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.ServerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        });

        private NetworkVariableQuaternion Rotation = new NetworkVariableQuaternion(new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.ServerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        });

        private Rigidbody rb;

        public float minZSpeed = 100;

        public float maxZSpeed = 500;

        private static float zRange;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            spaceRockPool = GameObject.FindWithTag(OBJECT_POOL_TAG).GetComponent<NetworkObjectPool>();
            Assert.IsNotNull(spaceRockPool);
            
            zRange = maxZSpeed - minZSpeed;
            gameController = GameObject.FindWithTag("GameController").GetComponent<GameController>();
        }

        

        public override void NetworkStart()
        {
            
            if (IsServer)
            {

                Rigid.Value.AddForce(0f,0f,-((Random.value * zRange) + minZSpeed));

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
            }
            else
            {
                rb.MovePosition(Rigid.Value.position);
                rb.MoveRotation(Rigid.Value.rotation);
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
            
            Rigid.Value.AddForce(0f,0f,-((Random.value * zRange) + minZSpeed));

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
        }

        public void FixedUpdate()
        {
            if (IsServer)
            {

                if (Rigid.Value.position.z <= -3)
                {
                    Yeet(true);
                }
                
                Position.Value = Rigid.Value.position;

                Rotation.Value = Rigid.Value.rotation;
            }
            else
            {
                rb.MovePosition(Rigid.Value.position);
                rb.MoveRotation(Rigid.Value.rotation);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer)
            {
                return;
            }
            if (other.tag.Equals("Player"))
            {
                gameController.ShipHit();
                Yeet(false);
            }
        }

        private void Yeet(bool avoided)
        {
            Assert.IsTrue(NetworkManager.IsServer);

            if (avoided)
            {
                gameController.GainedPoint(); // score an point if this is despawning because it was avoided
            }
            
            NetworkObject.Despawn();
            spaceRockPool.ReturnNetworkObject(NetworkObject);
        }
    }
}