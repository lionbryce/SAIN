using EFT;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover
{
    public enum RunStatus
    {
        None,
        FirstTurn,
        Running,
        Turning,
        ShortCorner,
        NoStamina,
        InteractingWithDoor,
        ArrivingAtDestination,
        CantSprint,
        LookAtEnemyNoSprint,
        Canceling,
    }

    public class SAINSprint : BotBase, IBotClass
    {
        public event Action<Vector3, Vector3> OnNewCornerMoveTo;

        public event Action<Vector3, Vector3> OnNewSprint;

        public event Action OnSprintEnd;

        public SAINSprint(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        public void Update()
        {
        }

        public bool Running => _runToPointCoroutine != null;

        public bool Canceling { get; private set; }

        public void CancelRun(float afterTime = -1f)
        {
            if (Running)
            {
                if (afterTime <= 0)
                {
                    StopRunCoroutine();
                    return;
                }
                if (!Canceling)
                {
                    Canceling = true;
                    Bot.StartCoroutine(CancelRunAfterTime(afterTime));
                }
            }
        }

        private void StopRunCoroutine()
        {
            if (!Running)
            {
                return;
            }
            OnSprintEnd?.Invoke();
            Canceling = false;
            Bot.StopCoroutine(_runToPointCoroutine);
            _runToPointCoroutine = null;
            Bot.Mover.Sprint(false);
            _path.Clear();
        }

        private IEnumerator CancelRunAfterTime(float afterTime)
        {
            yield return new WaitForSeconds(afterTime);
            StopRunCoroutine();
        }

        public bool RunToPointByWay(NavMeshPath way, ESprintUrgency urgency, bool stopSprintEnemyVisible, bool checkSameWay = true, System.Action callback = null)
        {
            if (!GetLastCorner(way, out Vector3 point))
            {
                return false;
            }
            ShallStopSprintWhenSeeEnemy = stopSprintEnemyVisible;
            if (checkSameWay && IsPointSameWay(point))
            {
                return true;
            }
            StartRun(way, point, urgency, callback);
            return true;
        }

        private bool GetLastCorner(NavMeshPath way, out Vector3 result)
        {
            Vector3[] corners = way?.corners;
            if (corners == null)
            {
                result = Vector3.zero;
                return false;
            }
            if (way.status != NavMeshPathStatus.PathComplete)
            {
                result = Vector3.zero;
                return false;
            }

            Vector3? last = corners.LastElement();
            if (last == null)
            {
                result = Vector3.zero;
                return false;
            }

            result = last.Value;
            return true;
        }

        public bool RunToPoint(Vector3 point, ESprintUrgency urgency, bool stopSprintEnemyVisible, bool checkSameWay = true, System.Action callback = null)
        {
            if (checkSameWay && IsPointSameWay(point))
            {
                return true;
            }

            if (!Bot.Mover.CanGoToPoint(point, out NavMeshPath path))
            {
                return false;
            }
            ShallStopSprintWhenSeeEnemy = stopSprintEnemyVisible;
            StartRun(path, point, urgency, callback);
            return true;
        }

        private bool ShallStopSprintWhenSeeEnemy;

        private bool IsPointSameWay(Vector3 point, float minDistSqr = 0.5f)
        {
            return Running && (LastRunDestination - point).sqrMagnitude < minDistSqr;
        }

        private void StartRun(NavMeshPath path, Vector3 point, ESprintUrgency urgency, System.Action callback)
        {
            StopRunCoroutine();
            BotOwner.AimingManager.CurrentAiming.LoseTarget();
            LastRunDestination = point;
            CurrentPath = path;
            _lastUrgency = urgency;
            _runToPointCoroutine = Bot.StartCoroutine(RunToPointCoroutine(path.corners, urgency, callback));
            OnNewSprint?.Invoke(path.corners[1], point);
        }

        private float _timeStartRun;

        private ESprintUrgency _lastUrgency;

        public NavMeshPath CurrentPath;

        public bool RecalcPath()
        {
            return RunToPoint(LastRunDestination, _lastUrgency, false);
        }

        public Vector3 LastRunDestination { get; private set; }

        private Coroutine _runToPointCoroutine;

        public RunStatus CurrentRunStatus { get; private set; }

        public Vector3 CurrentCornerDestination()
        {
            if (_path.Count <= _currentIndex)
            {
                return Vector3.zero;
            }
            return _path[_currentIndex];
        }

        private int _currentIndex = 0;

        private IEnumerator RunToPointCoroutine(Vector3[] corners, ESprintUrgency urgency, System.Action callback = null)
        {
            _path.Clear();
            _path.AddRange(corners);

            isShortCorner = false;
            _timeStartCorner = Time.time;
            positionMoving = true;
            _timeNotMoving = -1f;
            _timeStartRun = Time.time;

            BotOwner.Mover.Stop();
            _currentIndex = 1;

            // First step, look towards the path we want to run
            //yield return firstTurn(path.corners[1]);

            // Start running!
            yield return RunPath(urgency);

            callback?.Invoke();

            CurrentRunStatus = RunStatus.None;
            StopRunCoroutine();
        }

        private readonly List<Vector3> _path = new();

        private void MoveToNextCorner()
        {
            if (TotalCorners() > _currentIndex)
            {
                CheckCornerLength();
                _currentIndex++;
                Vector3 currentCorner = _path[_currentIndex];
                OnNewCornerMoveTo?.Invoke(currentCorner, LastRunDestination);
            }
        }

        private void CheckCornerLength()
        {
            Vector3 current = _path[_currentIndex];
            Vector3 next = _path[_currentIndex + 1];
            isShortCorner = (current - next).magnitude < 0.25f;
            _timeStartCorner = Time.time;
        }

        private float _timeStartCorner;

        private int TotalCorners()
        {
            return _path.Count - 1;
        }

        private Vector3 LastCorner()
        {
            int count = _path.Count;
            if (count == 0)
            {
                return Vector3.zero;
            }
            return _path[count - 1];
        }

        private static MoveSettings _moveSettings => SAINPlugin.LoadedPreset.GlobalSettings.Move;

        private IEnumerator RunPath(ESprintUrgency urgency)
        {
            int total = TotalCorners();
            for (int i = 1; i <= total; i++)
            {
                // Track distance to target corner in the path.
                float distToCurrent = float.MaxValue;
                while (distToCurrent > _moveSettings.BotSprintCornerReachDist)
                {
                    if (BotOwner.Mover.IsMoving)
                    {
                        BotOwner.Mover.Stop(); // Backwards sprint / moonwalking fix
                    }
                    distToCurrent = DistanceToCurrentCornerSqr();
                    DistanceToCurrentCorner = distToCurrent;

                    Vector3 current = CurrentCornerDestination();
                    if (SAINPlugin.DebugMode)
                    {
                        //DebugGizmos.Sphere(current, 0.1f);
                        //DebugGizmos.Line(current, Bot.Position, 0.1f, 0.1f);
                    }

                    // Start or stop sprinting with a buffer
                    HandleSprinting(distToCurrent, urgency);

                    Vector3 destination = CurrentCornerDestination();
                    if (!Bot.DoorOpener.Interacting &&
                        !Bot.DoorOpener.BreachingDoor)
                    {
                        TrackMovement();
                        float timeSinceNoMove = timeSinceNotMoving;
                        if (timeSinceNoMove > _moveSettings.BotSprintRecalcTime && Time.time - _timeStartRun > 2f)
                        {
                            RecalcPath();
                            yield break;
                        }
                        //else if (timeSinceNoMove > _moveSettings.BotSprintTryJumpTime)
                        //{
                        //    SAINBot.Mover.TryJump();
                        //}
                        else if (Bot.Info.FileSettings.Move.VAULT_TOGGLE
                            && GlobalSettingsClass.Instance.Move.VAULT_TOGGLE
                            && timeSinceNoMove > _moveSettings.BotSprintTryVaultTime)
                        {
                            Bot.Mover.TryVault();
                        }

                        Move((destination - Bot.Position).normalized);
                    }

                    float speed = IsSprintEnabled ? _moveSettings.BotSprintTurnSpeedWhileSprint : _moveSettings.BotSprintTurningSpeed;
                    float dotProduct = Steer(destination, speed);

                    //if (onLastCorner() &&
                    //    distToCurrent <= _moveSettings.BotSprintFinalDestReachDist)
                    //{
                    //    yield break;
                    //}

                    yield return null;
                }
                MoveToNextCorner();
            }
        }

        private bool isShortCorner;
        public float DistanceToCurrentCorner { get; private set; }

        private float FindStartSprintStamina(ESprintUrgency urgency)
        {
            switch (urgency)
            {
                case ESprintUrgency.None:
                case ESprintUrgency.Low:
                    return 0.75f;

                case ESprintUrgency.Middle:
                    return 0.5f;

                case ESprintUrgency.High:
                    return 0.2f;

                default:
                    return 0.5f;
            }
        }

        private float FindEndSprintStamina(ESprintUrgency urgency)
        {
            switch (urgency)
            {
                case ESprintUrgency.None:
                case ESprintUrgency.Low:
                    return 0.4f;

                case ESprintUrgency.Middle:
                    return 0.2f;

                case ESprintUrgency.High:
                    return 0.01f;

                default:
                    return 0.25f;
            }
        }

        private bool ShallLookAtEnemy()
        {
            return ShallStopSprintWhenSeeEnemy && Bot.Enemy?.IsVisible == true;
        }

        private void HandleSprinting(float distToCurrent, ESprintUrgency urgency)
        {
            // I cant sprint :(
            if (!Player.MovementContext.CanSprint)
            {
                CurrentRunStatus = RunStatus.CantSprint;
                return;
            }

            if (Canceling)
            {
                CurrentRunStatus = RunStatus.Canceling;
                Bot.Mover.EnableSprintPlayer(false);
                return;
            }

            if (ShallLookAtEnemy())
            {
                CurrentRunStatus = RunStatus.LookAtEnemyNoSprint;
                Bot.Mover.EnableSprintPlayer(false);
                return;
            }

            if (isShortCorner)
            {
                CurrentRunStatus = RunStatus.ShortCorner;
                Bot.Mover.EnableSprintPlayer(false);
                return;
            }

            if (Player.IsSprintEnabled)
            {
                if (Bot.IsCheater)
                {
                    Player.MovementContext.SprintSpeed = 50f;
                }
                else if (_moveSettings.EditSprintSpeed)
                {
                    Player.MovementContext.SprintSpeed = 1.5f;
                }
            }

            // Were messing with a door, dont sprint
            if (Bot.DoorOpener.ShallPauseSprintForOpening())
            {
                CurrentRunStatus = RunStatus.InteractingWithDoor;
                Bot.Mover.EnableSprintPlayer(false);
                return;
            }

            // We are arriving to our destination, stop sprinting when you get close.
            if ((LastCorner() - BotPosition).magnitude <= _moveSettings.BotSprintDistanceToStopSprintDestination)
            {
                Bot.Mover.EnableSprintPlayer(false);
                CurrentRunStatus = RunStatus.ArrivingAtDestination;
                return;
            }

            float staminaValue = Player.Physical.Stamina.NormalValue;

            // We are out of stamina, stop sprinting.
            if (ShallPauseSprintStamina(staminaValue, urgency))
            {
                Bot.Mover.EnableSprintPlayer(false);
                CurrentRunStatus = RunStatus.NoStamina;
                return;
            }

            // We are approaching a sharp corner, or we are currently not looking in the direction we need to go, stop sprinting
            if (ShallPauseSprintAngle())
            {
                Bot.Mover.EnableSprintPlayer(false);
                CurrentRunStatus = RunStatus.Turning;
                return;
            }

            // If we arne't already sprinting, and our corner were moving to is far enough away, and I have enough stamina, and the angle isn't too sharp... enable sprint
            if (ShallStartSprintStamina(staminaValue, urgency) &&
                _timeStartCorner + 0.25f < Time.time)
            {
                Bot.Mover.EnableSprintPlayer(true);
                CurrentRunStatus = RunStatus.Running;
                return;
            }
        }

        private bool ShallPauseSprintStamina(float stamina, ESprintUrgency urgency) => stamina <= FindEndSprintStamina(urgency);

        private bool ShallStartSprintStamina(float stamina, ESprintUrgency urgency) => stamina >= FindStartSprintStamina(urgency);

        private bool ShallPauseSprintAngle()
        {
            Vector3? currentCorner = this.CurrentCornerDestination();
            return currentCorner != null && CheckShallPauseSprintFromTurn(currentCorner.Value, _moveSettings.BotSprintCurrentCornerAngleMax);
        }

        private bool CheckShallPauseSprintFromTurn(Vector3 currentCorner, float angleThresh = 25f)
        {
            return FindAngleFromLook(currentCorner) >= angleThresh;
        }

        private float FindAngleFromLook(Vector3 end)
        {
            Vector3 origin = BotOwner.WeaponRoot.position;
            Vector3 aDir = Bot.LookDirection;
            Vector3 bDir = end - origin;
            aDir.y = 0;
            bDir.y = 0;
            return Vector3.Angle(aDir, bDir);
        }

        private void TrackMovement()
        {
            if (nextCheckPosTime < Time.time)
            {
                nextCheckPosTime = Time.time + _moveSettings.BotSprintNotMovingCheckFreq;
                Vector3 botPos = BotPosition;
                positionMoving = (botPos - lastCheckPos).sqrMagnitude > _moveSettings.BotSprintNotMovingThreshold;
                if (positionMoving)
                {
                    _timeNotMoving = -1f;
                    lastCheckPos = botPos;
                }
                else if (_timeNotMoving < 0)
                {
                    _timeNotMoving = Time.time;
                }
            }
        }

        private bool positionMoving;
        private Vector3 lastCheckPos;
        private float nextCheckPosTime;
        private float timeSinceNotMoving => positionMoving ? 0f : Time.time - _timeNotMoving;
        private float _timeNotMoving;

        private Vector3 BotPosition
        {
            get
            {
                Vector3 botPos = Bot.Position;
                if (NavMesh.SamplePosition(botPos, out var hit, 0.5f, -1))
                {
                    botPos = hit.position;
                }
                return botPos;
            }
        }

        private float DistanceToCurrentCornerSqr()
        {
            Vector3 destination = CurrentCornerDestination();
            Vector3 testPoint = destination + Vector3.up;
            if (Physics.Raycast(testPoint, Vector3.down, out var hit, 1.5f, LayerMaskClass.HighPolyWithTerrainMask))
            {
                destination = hit.point;
            }
            return (destination - BotPosition).sqrMagnitude;
        }

        private float Steer(Vector3 target, float turnSpeed)
        {
            Vector3 playerPosition = Bot.Position;
            Vector3 currentLookDirNormal = Bot.Person.Transform.LookDirection.normalized;
            target += Vector3.up;
            Vector3 targetLookDir = (target - playerPosition);
            Vector3 targetLookDirNormal = targetLookDir.normalized;

            if (!Bot.DoorOpener.Interacting)
            {
                if (ShallLookAtEnemy())
                {
                    Bot.Steering.LookToEnemy(Bot.Enemy);
                }
                else if (!ShallSteerbyPriority() || !Bot.Steering.SteerByPriority(null, false, true))
                {
                    Bot.Steering.LookToDirection(targetLookDirNormal, true, turnSpeed);
                }
            }
            float dotProduct = Vector3.Dot(targetLookDirNormal, currentLookDirNormal);
            return dotProduct;
        }

        private bool ShallSteerbyPriority()
        {
            return CurrentRunStatus switch
            {
                RunStatus.Turning or 
                RunStatus.FirstTurn or 
                RunStatus.Running or 
                RunStatus.ShortCorner => false,
                _ => true,
            };
        }

        private void Move(Vector3 direction)
        {
            if (Bot.IsCheater)
            {
                direction *= 10f;
            }
            Player.CharacterController.SetSteerDirection(direction);
            BotOwner.AimingManager.CurrentAiming.Move(Player.Speed);
            if (BotOwner.Mover != null)
            {
                BotOwner.Mover.IsMoving = true;
            }
            Player.Move(FindMoveDirection(direction));
        }

        public Vector2 FindMoveDirection(Vector3 direction)
        {
            Vector3 vector = Quaternion.Euler(0f, 0f, Player.Rotation.x) * new Vector2(direction.x, direction.z);
            return new Vector2(vector.x, vector.y);
        }

        public void Dispose()
        {
        }

        private bool IsSprintEnabled => Player.IsSprintEnabled;
    }

    public enum ESprintUrgency
    {
        None = 0,
        Low = 1,
        Middle = 2,
        High = 3,
    }
}