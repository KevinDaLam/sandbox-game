using UnityEngine;

namespace Game.Platformer
{
    public struct PlayerInputState
    {
        public Vector2 Move;
        public float YawDelta;
        public bool JumpPressed;
        public bool JumpHeld;
        public bool Sprint;
    }
}
