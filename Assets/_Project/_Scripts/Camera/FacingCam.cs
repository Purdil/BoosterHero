using System.Collections.Generic;
using UnityEngine;

namespace _Project._Scripts.Camera
{
    public enum Facing
    {
        Front,
        Right,
        Back,
        Left,
    }
    public class FacingCam : MonoBehaviour
    {
        [SerializeField] private float[] facingAngles;

        private Facing _currentFacing = Facing.Front;
        public void SetFacing(float xSpeed,float xDir)
        {
            if (xDir > 0 && xSpeed > 0)
                _currentFacing += 1;
            else if(xDir < 0 && xSpeed > 0)
                _currentFacing -= 1;
            
            
        }
    }
}