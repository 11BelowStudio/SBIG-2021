using System;
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

        private GameController gameManager;

        public static int SpaceRockCount = 0;
        
        private static readonly string OBJECT_POOL_TAG = "NetworkObjectPool";
        
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
            gameManager = GameObject.FindWithTag("GameController").GetComponent<GameController>();
        }

        private void Start()
        {
            SpaceRockCount += 1;

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

        public void FixedUpdate()
        {
            if (IsServer)
            {

                if (Rigid.Value.position.z <= -3)
                {
                    Yeet();
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
                gameManager.ShipHit();
                Yeet();
            }
        }

        private void Yeet()
        {
            Assert.IsTrue(NetworkManager.IsServer);
            
            NetworkObject.Despawn();
            spaceRockPool.ReturnNetworkObject(NetworkObject);
            SpaceRockCount -= 1;
        }
    }
}