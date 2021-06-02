using System;
using System.Linq;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace Game
{
    public class TunnelMovingScript: MonoBehaviour
    {
        /// <summary>
        /// The tunnel pieces
        /// </summary>
        public GameObject[] tunnelPieces;

        /// <summary>
        /// How fast the Z speed of the tunnel should be whilst waiting for game to start/after game over
        /// </summary>
        public float notInGameZSpeedScale;

        /// <summary>
        /// movement vector whilst not in game
        /// </summary>
        private Vector3 notInGameMovementVector;
        
        /// <summary>
        /// How fast the Z speed of the tunnel should be whilst in game
        /// </summary>
        public float inGameZSpeedScale;
        
        /// <summary>
        /// movement vector whilst in game
        /// </summary>
        private Vector3 inGameMovementVector;

        /// <summary>
        /// How fast the tunnel pieces should move back
        /// </summary>
        public float tunnelPieceMovementSpeed;

        /// <summary>
        /// Size of the pieces in the Z axis
        /// </summary>
        public float tunnelPieceZSize;

        /// <summary>
        /// Gamecontroller
        /// </summary>
        private GameController gc;

        /// <summary>
        /// How far the pieces should move (unscaled)
        /// </summary>
        private Vector3 tunnelPieceMovementVector;

        /// <summary>
        /// If the tunnel pieces reach this Z position, they need to be moved forward
        /// </summary>
        private float moveBackAtThisZ;
        
        /// <summary>
        /// How far should the tunnel pieces be moved forward by to reset them?
        /// </summary>
        private Vector3 moveForwardVector;

        /// <summary>
        /// How many pieces are there?
        /// </summary>
        private int pieceCount;

        public void Start()
        {

            gc = GameController.Singleton;
            
            gc.hasGameStarted.OnValueChanged += OnGameStartedChanged;
            gc.isGameOver.OnValueChanged += OnGameOverChanged;

            notInGameMovementVector = new Vector3(0, 0, notInGameZSpeedScale * -tunnelPieceZSize);
            
            Debug.Log($"Out of game movement vector is {tunnelPieceMovementVector}");

            inGameMovementVector = new Vector3(0, 0, inGameZSpeedScale * -tunnelPieceZSize);
            
            Debug.Log($"Ingame movement vector is {tunnelPieceMovementVector}");

            tunnelPieceMovementVector = notInGameMovementVector;

            //tunnelPieceMovementSpeed = tunnelPieceZSize * 2;

            //tunnelPieceMovementVector = new Vector3(0, 0, -tunnelPieceMovementSpeed);
            
            //Debug.Log($"Movement vector is {tunnelPieceMovementVector}");

            pieceCount = tunnelPieces.Count();
            
            Debug.Log($"There are {pieceCount} tunnel pieces.");

            moveBackAtThisZ = 0 - tunnelPieceZSize;
            
            Debug.Log($"Moves forward at z={moveBackAtThisZ}");

            moveForwardVector = new Vector3(0, 0, pieceCount * tunnelPieceZSize);
            
            Debug.Log($"Movement forward vector is {moveForwardVector}");
        }

        public void Update()
        {
            // TODO: only move them in Z axis when game isn't over.

            Vector3 thisMove = tunnelPieceMovementVector * Time.deltaTime;
            
            Debug.Log(thisMove);

            for (int i = (pieceCount-1); i >= 0; i--)
            {
                
                Transform t = tunnelPieces[i].transform;

                //t.position += thisMove;

                t.Translate(thisMove);

                if (t.position.z <= moveBackAtThisZ)
                {
                    //t.position += moveForwardVector;
                    t.Translate(moveForwardVector);
                }
                

            }


        }
        
        private void OnGameStartedChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                tunnelPieceMovementVector = inGameMovementVector;
            }
            else
            {
                tunnelPieceMovementVector = notInGameMovementVector;
            }
        }
        
        private void OnGameOverChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                tunnelPieceMovementVector = notInGameMovementVector;
            }
            else
            {
                tunnelPieceMovementVector = inGameMovementVector;
            }
        }
    }
}