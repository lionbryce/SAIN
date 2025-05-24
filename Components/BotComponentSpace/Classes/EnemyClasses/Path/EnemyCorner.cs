using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class EnemyCorner(Vector3 groundPoint, float signedAngle, int pathIndex)
    {
        public int PathIndex { get; } = pathIndex;
        public Vector3 GroundPosition { get; } = groundPoint;
        public float SignedAngleToTarget { get; } = signedAngle;

        public Vector3 EyeLevelCorner(Vector3 eyePos, Vector3 botPosition)
        {
            return CornerHelpers.EyeLevelCorner(eyePos, botPosition, GroundPosition);
        }

        public Vector3 PointPastCorner(Vector3 eyePos, Vector3 botPosition)
        {
            if (_nextLookPointTime < Time.time)
            {
                _nextLookPointTime = Time.time + LOOK_POINT_FREQUENCY;
                _blindCornerLookPoint = CornerHelpers.PointPastEyeLevelCorner(eyePos, botPosition, GroundPosition);
            }
            return _blindCornerLookPoint;
        }

        private Vector3 _blindCornerLookPoint = groundPoint;
        private float _nextLookPointTime = 0f;
        const float LOOK_POINT_FREQUENCY = 1f / 30f;
    }
}