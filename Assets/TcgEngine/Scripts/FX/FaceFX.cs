using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;

namespace TcgEngine.FX
{
    /// <summary>
    /// Rotate FX to face camera
    /// </summary>

    public class FaceFX : MonoBehaviour
    {
        public FaceType type;

        void Start()
        {
            if (type == FaceType.FaceCamera)
            {
                GameCamera cam = GameCamera.Get();
                if (cam != null)
                {
                    Vector3 forward = cam.transform.forward;
                    transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                }
            }

            if (type == FaceType.FaceBoard)
            {
                GameBoard board = GameBoard.Get();
                if (board != null)
                {
                    Vector3 forward = board.transform.forward;
                    transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                }
            }
        }
    }

    public enum FaceType
    {
        FaceCamera = 0,
        FaceBoard = 10
    }
}
