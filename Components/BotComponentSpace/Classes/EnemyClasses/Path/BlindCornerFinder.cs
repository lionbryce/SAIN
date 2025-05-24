using SAIN.Helpers;
using SAIN.Models.Enums;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses
{
    public class BlindCornerFinder(Enemy enemy) : EnemyBase(enemy)
    {
        private const float HEIGHT = 1.6f;
        private const float HEIGHT_HALF = HEIGHT / 2f;
        private Vector3 HEIGHT_OFFSET = Vector3.up * HEIGHT;
        private Vector3 HEIGHT_OFFSET_HALF = Vector3.up * HEIGHT_HALF;
        private readonly List<Vector3> _corners = [];

        private readonly List<Vector3> _segments = new();
        private const float SEGMENT_LENGTH = 0.5f;
        private const float SEGMENT_LENGTH_SQR = SEGMENT_LENGTH * SEGMENT_LENGTH;

        public void ClearBlindCorner()
        {
            Enemy.Path.EnemyCorners.Remove(ECornerType.Blind);
        }

        public IEnumerator FindBlindCorner(Vector3[] corners, Vector3 enemyPosition)
        {
            _corners.Clear();
            _corners.AddRange(corners);
            int count = _corners.Count;

            if (count <= 2)
            {
                ClearBlindCorner();
                yield break;
            }

            int blindCornerIndex = count - 1;
            Vector3? blindCorner = null;

            for (int i = count - 1; i > 0; i--)
            {
                Vector3 corner = _corners[i];
                blindCornerIndex = i - 1;
                Vector3 nextCorner = _corners[i - 1];

                _segments.Clear();
                FindSegmentsBetweenCorner(corner, nextCorner, _segments);

                for (int j = 0; j < _segments.Count; j++)
                {
                    Vector3 segment = _segments[j];
                    if (CheckSightAtSegment(segment, Bot.Transform.EyePosition, out Vector3 sightPoint))
                    {
                        blindCorner = segment;
                        break;
                    }

                    yield return null;
                }
                if (blindCorner != null)
                {
                    break;
                }
            }

            if (blindCorner != null)
            {
                Vector3 blindCornerDir = (blindCorner.Value - Bot.Transform.EyePosition).normalized;
                blindCornerDir.y = 0;
                Vector3 enemyPosDir = (enemyPosition - Bot.Transform.EyePosition).normalized;
                enemyPosDir.y = 0;

                float signedAngle = Vector3.SignedAngle(blindCornerDir, enemyPosDir, Vector3.up);
                Enemy.Path.EnemyCorners.AddOrReplace(ECornerType.Blind, new EnemyCorner(blindCorner.Value, signedAngle, blindCornerIndex));
                yield break;
            }
            ClearBlindCorner();
        }

        private bool CheckSightAtSegment(Vector3 segment, Vector3 origin, out Vector3 sightPoint)
        {
            Vector3 first = segment + (Vector3.up * 0.1f);
            Vector3 firstDir = first - origin;
            DebugGizmos.Sphere(first, 0.1f, Color.blue, true, 10f);

            if (!Physics.Raycast(origin, firstDir, firstDir.magnitude, LayerMaskClass.HighPolyWithTerrainMaskAI))
            {
                sightPoint = segment;
                DebugGizmos.Line(origin, sightPoint, Color.blue, 0.05f, true, 10f, false);
                return true;
            }

            Vector3 second = segment + HEIGHT_OFFSET_HALF;
            Vector3 secondDir = second - origin;
            if (!Physics.Raycast(origin, secondDir, secondDir.magnitude, LayerMaskClass.HighPolyWithTerrainMaskAI))
            {
                sightPoint = second;
                DebugGizmos.Line(origin, sightPoint, Color.blue, 0.05f, true, 10f, false);
                return true;
            }

            Vector3 third = segment + HEIGHT_OFFSET;
            Vector3 thirdDir = third - origin;
            if (!Physics.Raycast(origin, thirdDir, thirdDir.magnitude, LayerMaskClass.HighPolyWithTerrainMaskAI))
            {
                sightPoint = third;
                DebugGizmos.Line(origin, sightPoint, Color.blue, 0.025f, true, 10f, false);
                return true;
            }

            sightPoint = Vector3.zero;
            return false;
        }

        private void FindSegmentsBetweenCorner(Vector3 corner, Vector3 nextCorner, List<Vector3> segmentsList)
        {
            segmentsList.Add(corner);
            Vector3 cornerDirection = (nextCorner - corner);
            float sqrMag = cornerDirection.sqrMagnitude;
            if (sqrMag <= SEGMENT_LENGTH_SQR)
            {
                return;
            }
            if (sqrMag <= SEGMENT_LENGTH_SQR * 2f)
            {
                segmentsList.Add(Vector3.Lerp(corner, nextCorner, 0.5f));
                return;
            }
            float segmentLength = sqrMag / SEGMENT_LENGTH_SQR;
            Vector3 segmentDir = cornerDirection.normalized * segmentLength;
            int segmentCount = Mathf.RoundToInt(segmentLength);
            Vector3 segmentPoint = corner;
            for (int i = 0; i < segmentCount; i++)
            {
                segmentPoint += segmentDir;
                segmentsList.Add(segmentPoint);
            }
        }

        public static Vector3 RaycastPastCorner(Vector3 corner, Vector3 lookPoint, float addHeight, float addDistance = 2f)
        {
            corner.y += addHeight;
            Vector3 cornerDir = corner - lookPoint;

            Vector3 farPoint;
            if (Physics.Raycast(lookPoint, cornerDir, out var hit, addDistance, LayerMaskClass.HighPolyWithTerrainMask))
            {
                farPoint = hit.point;
            }
            else
            {
                farPoint = corner + cornerDir.normalized * addDistance;
            }
            Vector3 midPoint = Vector3.Lerp(farPoint, corner, 0.5f);
            return midPoint;
        }
    }
}