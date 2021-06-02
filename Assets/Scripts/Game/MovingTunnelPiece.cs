using UnityEngine;

namespace Game
{
    public class MovingTunnelPiece: MonoBehaviour
    {

        public Vector3 MoveThis(Vector3 moveBy)
        {
            transform.Translate(moveBy, Space.World);
            return transform.position;
        }
    }
}