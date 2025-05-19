using EFT;
using SAIN.Helpers;
using SAIN.Models.Enums;
using SAIN.Models.Structs;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Search
{
    public enum EPathCalcFailReason
    {
        None,
        NullDestination,
        NoTarget,
        NullPlace,
        TooClose,
        SampleStart,
        SampleEnd,
        CalcPath,
        LastCorner,
    }

    public class SearchPathFinder : BotSubClass<SAINSearchClass>
    {
        public Vector3 FinalDestination { get; private set; }
        public EnemyPlace TargetPlace { get; private set; }
        public BotPeekPlan? PeekPoints { get; private set; }
        public bool SearchedTargetPosition { get; private set; }
        public bool FinishedPeeking { get; set; }

        private string _failReason;

        public SearchPathFinder(SAINSearchClass searchClass) : base(searchClass)
        {
            _searchPath = new NavMeshPath();
        }

        public bool HasPathToSearchTarget(Enemy enemy, out string failReason)
        {
            if (_nextCheckSearchTime < Time.time)
            {
                _nextCheckSearchTime = Time.time + 1f;
                _canStartSearch = CalculatePath(enemy, out failReason);
                _failReason = failReason;
            }
            failReason = _failReason;
            return _canStartSearch;
        }

        public void UpdateSearchDestination(Enemy enemy)
        {
            checkFinishedSearch(enemy);

            if (_nextCheckPosTime < Time.time || SearchedTargetPosition || FinishedPeeking || TargetPlace == null)
            {
                _nextCheckPosTime = Time.time + 4f;
                if (!CalculatePath(enemy, out string failReason))
                {
                    //Logger.LogDebug($"Failed to calc path during search for reason: [{failReason}]");
                }
            }
        }

        private void checkFinishedSearch(Enemy enemy)
        {
            if (SearchedTargetPosition)
            {
                return;
            }
            var lastKnown = enemy.KnownPlaces.LastKnownPlace;
            if (lastKnown == null)
            {
                Reset();
                return;
            }
            if (lastKnown.HasArrivedPersonal || lastKnown.HasArrivedSquad)
            {
                Reset();
                return;
            }

            var pathToEnemy = enemy.Path.PathToEnemy;
            if (pathToEnemy.corners.Length > 2)
            {
                return;
            }

            if ((FinalDestination - Bot.Position).sqrMagnitude > 0.75)
            {
                return;
            }

            var lastCorner = pathToEnemy.LastCorner();
            if (lastCorner == null)
            {
                Reset();
                return;
            }

            if ((lastCorner.Value - FinalDestination).sqrMagnitude < 1f)
            {
                SearchedTargetPosition = true;
                enemy.KnownPlaces.SetPlaceAsSearched(lastKnown);
                Reset();
                return;
            }
            if (!CalculatePath(enemy, out string failReason))
            {
                Logger.LogDebug($"Failed to calc path during search for reason: [{failReason}]");
                Reset();
                return;
            }
        }

        public void Reset()
        {
            _searchPath.ClearCorners();
            PeekPoints?.DisposeDebug();
            PeekPoints = null;
            TargetPlace = null;
            FinishedPeeking = false;
            SearchedTargetPosition = false;
        }

        public bool CalculatePath(Enemy enemy, out string failReason)
        {
            Vector3? lastPathPoint = enemy.Path.PathToEnemy.LastCorner() ?? enemy.KnownPlaces.LastKnownPlace?.Position;
            if (lastPathPoint == null)
            {
                failReason = "lastPathPoint Null";
                return false;
            }

            Vector3 point = lastPathPoint.Value;
            Vector3 start = Bot.Position;
            if ((point - start).sqrMagnitude <= 0.25f)
            {
                failReason = "tooClose";
                return false;
            }

            _searchPath.ClearCorners();
            if (!NavMesh.CalculatePath(start, point, -1, _searchPath))
            {
                failReason = "pathInvalid";
                return false;
            }
            Vector3? lastCorner = _searchPath.LastCorner();
            if (lastCorner == null)
            {
                failReason = "lastCornerNull";
                return false;
            }

            BaseClass.Reset();
            FinalDestination = lastCorner.Value;
            PeekPoints = findPeekPosition(enemy);
            TargetPlace = enemy.KnownPlaces.LastKnownPlace;
            failReason = string.Empty;
            return true;
        }

        public bool CalculatePath(EnemyPlace place, out EPathCalcFailReason failReason)
        {
            if (place == null)
            {
                failReason = EPathCalcFailReason.NullPlace;
                return false;
            }

            Vector3 point = place.Position;
            Vector3 start = Bot.Position;
            if ((point - start).sqrMagnitude <= 0.5f)
            {
                failReason = EPathCalcFailReason.TooClose;
                return false;
            }

            if (!NavMesh.SamplePosition(point, out var endHit, 1f, -1))
            {
                failReason = EPathCalcFailReason.SampleEnd;
                return false;
            }
            if (!NavMesh.SamplePosition(start, out var startHit, 1f, -1))
            {
                failReason = EPathCalcFailReason.SampleStart;
                return false;
            }
            _searchPath.ClearCorners();
            if (!NavMesh.CalculatePath(startHit.position, endHit.position, -1, _searchPath) || _searchPath.status == NavMeshPathStatus.PathPartial)
            {
                failReason = EPathCalcFailReason.CalcPath;
                return false;
            }
            Vector3? lastCorner = _searchPath.LastCorner();
            if (lastCorner == null)
            {
                failReason = EPathCalcFailReason.LastCorner;
                return false;
            }

            BaseClass.Reset();
            //PeekPoints = findPeekPosition(startHit.position);
            TargetPlace = place;
            failReason = EPathCalcFailReason.None;
            return true;
        }

        private BotPeekPlan? findPeekPosition(Enemy enemy)
        {
            const float MIN_ANGLE_TO_PEEK = 5f;
            const float CORNER_PEEK_DIST = 3f;
            if (!enemy.Path.EnemyCorners.TryGetValue(ECornerType.Blind, out EnemyCorner blindCorner))
            {
                return null;
            }

            Vector3[] pathCorners = enemy.Path.PathToEnemy.corners;
            int count = pathCorners.Length;
            int blindCornerIndex = blindCorner.PathIndex;
            Vector3 blindCornerPosition = blindCorner.GroundPosition;
            Vector3 botPosition = Bot.Position;
            Vector3 blindCornerDir = blindCornerPosition - botPosition;
            Vector3 blindCornerDirNormal = blindCornerDir.normalized;

            Vector3 startPeekPosition = blindCornerPosition - (blindCornerDirNormal * CORNER_PEEK_DIST);

            for (int i = blindCornerIndex; i < count; i++)
            {
                Vector3 corner = pathCorners[i];
                Vector3 dir = corner - blindCornerPosition;
                Vector3 dirNormal = dir.normalized;
                float signedAngle = findHorizSignedAngle(blindCornerDirNormal, dirNormal);
                if (Mathf.Abs(signedAngle) < MIN_ANGLE_TO_PEEK)
                {
                    continue;
                }
                Vector3 oppositePoint = blindCornerPosition - (dirNormal * CORNER_PEEK_DIST);
                if (NavMesh.Raycast(blindCornerPosition, oppositePoint, out NavMeshHit hit, -1))
                {
                    oppositePoint = hit.position;
                }

                return new BotPeekPlan(startPeekPosition, oppositePoint, corner);
            }
            return null;
        }

        private float findHorizSignedAngle(Vector3 dirA, Vector3 dirB)
        {
            dirA.y = 0;
            dirB.y = 0;
            float signedAngle = Vector3.SignedAngle(dirA, dirB, Vector3.up);
            return signedAngle;
        }

        private bool _canStartSearch;
        private float _nextCheckSearchTime;
        private float _nextCheckPosTime;
        private NavMeshPath _searchPath;
    }
}