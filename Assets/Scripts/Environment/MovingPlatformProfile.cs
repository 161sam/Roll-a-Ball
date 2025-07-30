using UnityEngine;

namespace RollABall.Environment
{
    /// <summary>
    /// ScriptableObject profile that stores MovingPlatform parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "MovingPlatformProfile", menuName = "Roll-a-Ball/Moving Platform Profile")]
    public class MovingPlatformProfile : ScriptableObject
    {
        public Vector3 startPosition = Vector3.zero;
        public Vector3 endPosition = Vector3.up * 3f;
        public float moveSpeed = 2f;
        public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public MovingPlatform.MovementType movementType = MovingPlatform.MovementType.Linear;
        public bool useLocalSpace = true;
        public float pauseDuration = 1f;
        public bool startMovingImmediately = true;
        [Header("Audio")]
        public AudioClip mechanicalSound;
        public bool playAudioOnMovement = true;
    }
}
