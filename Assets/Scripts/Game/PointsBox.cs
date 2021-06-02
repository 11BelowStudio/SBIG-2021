using System;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Extensions;
using MLAPI.NetworkVariable;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace Game
{
    public class PointsBox: NetworkBehaviour, IAmYeetable
    {
        private NetworkObjectPool objectPool;

        private GameController gc;

        private static readonly string OBJECT_POOL_TAG = "NetworkObjectPool";

        private NetworkVariableBool Exists = new NetworkVariableBool(new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.ServerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        }, false);
        
        
        public NetworkVariableVector3 Position = new NetworkVariableVector3(new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.ServerOnly,
            ReadPermission = NetworkVariablePermission.Everyone
        });
        
        private NetworkVariable<Rigidbody> Rigid = new NetworkVariable<Rigidbody>(new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.ServerOnly,
            ReadPermission = NetworkVariablePermission.ServerOnly
        });

        private Rigidbody rb;

        public float minZSpeed = 100;

        public float maxZSpeed = 200;

        private static float zRange;
        
        private NetworkVariableVector3 ForceToAddEveryFrame = new NetworkVariableVector3(new NetworkVariableSettings
        {
            WritePermission = NetworkVariablePermission.ServerOnly,
            ReadPermission = NetworkVariablePermission.ServerOnly
        });

        private void Awake()
        {
            
            objectPool = GameObject.FindWithTag(OBJECT_POOL_TAG).GetComponent<NetworkObjectPool>();
            Assert.IsNotNull(objectPool);
            rb = GetComponent<Rigidbody>();
            if (IsServer)
            {
                Rigid.Value = rb;
            }
            zRange = maxZSpeed - minZSpeed;
        }

        

        public override void NetworkStart()
        {
            gc = GameObject.FindWithTag("GameController").GetComponent<GameController>();
            if (IsServer)
            {
                
            }
            else
            {
                rb.position = Position.Value;
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
            
            
            Exists.Value = true;
        }
        
        public void CreatedFromPool(float startZ, float startRadius)
        {
            Assert.IsTrue(IsServer);

            Vector3 startPos = Random.insideUnitSphere * startRadius;

            //Debug.Log($"Initial start: {startPos}");
            
            startPos += new Vector3(0, 0, startZ);
            
            //Debug.Log($"Moved start: {startPos}");
            
            transform.position = startPos;
            
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

            Position.Value = startPos;

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

                if (Rigid.Value.position.z <= -5)
                {
                    Yeet();
                }

                Rigid.Value.AddForce(ForceToAddEveryFrame.Value);
                
                Position.Value = Rigid.Value.position;
            }
            else
            {
                rb.MovePosition(Position.Value);
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
                gc.HitPointsBox();
                Yeet();
            }
        }

        public void Yeet(bool whoCares)
        {
            Yeet();
        }

        private void Yeet()
        {
            Assert.IsTrue(NetworkManager.IsServer);

            Exists.Value = false;
            
            NetworkObject.Despawn();
            objectPool.ReturnNetworkObject(NetworkObject);
        }
    }
}