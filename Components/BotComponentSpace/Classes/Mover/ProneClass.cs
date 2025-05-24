using EFT;
using HarmonyLib;
using SAIN.Helpers;
using SAIN.Models.Enums;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using System.Reflection;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class ProneClass : BotBase, IBotClass
    {
        private float _nextChangeProneTime { get; set; }
        private bool _canshoot { get; set; }
        private float _nextCheckShootTime { get; set; }

        public ProneClass(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }

        public void SetProne(bool value)
        {
            BotOwner.BotLay.IsLay = value;
        }

        public bool ShallProne(bool withShoot, float mindist = 25f)
        {
            if (!Bot.Info.FileSettings.Move.PRONE_TOGGLE || !GlobalSettingsClass.Instance.Move.PRONE_TOGGLE)
            {
                return false;
            }
            if (Player.MovementContext.CanProne)
            {
                var enemy = Bot.Enemy;
                if (enemy != null)
                {
                    float distance = (enemy.EnemyPosition - Bot.Position).sqrMagnitude;
                    if (distance > mindist * mindist)
                    {
                        if (withShoot)
                        {
                            return CanShootFromProne(enemy.EnemyPosition);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public bool ShallProneHide(float mindist = 10f)
        {
            if (!Bot.Info.FileSettings.Move.PRONE_TOGGLE || !GlobalSettingsClass.Instance.Move.PRONE_TOGGLE)
            {
                return false;
            }
            if (_nextChangeProneTime > Time.time)
            {
                return Player.IsInPronePose;
            }

            if (!Player.MovementContext.CanProne)
            {
                return false;
            }

            Enemy enemy = Bot.Enemy;
            if (enemy == null)
            {
                return false;
            }

            Vector3? lastKnownPos = enemy.LastKnownPosition;
            if (lastKnownPos == null)
            {
                return false;
            }
            if (Bot.CurrentTargetDistance < mindist)
            {
                return false;
            }

            bool isUnderDuress = Bot.Decision.CurrentSelfDecision != ESelfDecision.None || Bot.Suppression.IsHeavySuppressed;
            bool shallProne = isUnderDuress || !CheckShootProne(lastKnownPos.Value, enemy);
            if (shallProne)
            {
                _nextChangeProneTime = Time.time + 3f;
            }
            return shallProne;
        }

        private bool CheckShootProne(Vector3? lastKnownPos, Enemy enemy)
        {
            if (_nextCheckShootTime > Time.time)
            {
                return _canshoot;
            }
            _nextCheckShootTime = Time.time + 0.5f;

            var blindCorner = enemy.Path.EnemyCorners.EyeLevelPosition(ECornerType.Blind);
            if (blindCorner != null)
            {
                _canshoot = CanShootFromProne(blindCorner.Value);
            }
            else
            {
                _canshoot = CanShootFromProne(lastKnownPos.Value);
            }
            return _canshoot;
        }

        public bool ShallGetUp(float mindist = 30f)
        {
            if (BotOwner.BotLay.IsLay)
            {
                var enemy = Bot.Enemy;
                if (enemy == null)
                {
                    return true;
                }
                float distance = (enemy.EnemyPosition - Bot.Transform.Position).magnitude;
                if (distance > mindist)
                {
                    return !IsChestPosVisible(enemy.EnemyHeadPosition);
                }
            }
            return false;
        }

        public bool IsChestPosVisible(Vector3 enemyHeadPos)
        {
            Vector3 botPos = Bot.Transform.Position;
            botPos += Vector3.up * 1f;
            Vector3 direction = botPos - enemyHeadPos;
            return !Physics.Raycast(enemyHeadPos, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask);
        }

        public bool CanShootFromProne(Vector3 target)
        {
            Vector3 vector = Bot.Transform.Position + Vector3.up * 0.14f;
            Vector3 vector2 = target + Vector3.up - vector;
            Vector3 from = vector2;
            from.y = vector.y;
            float num = Vector3.Angle(from, vector2);
            float lay_DOWN_ANG_SHOOT = HelpersGClass.LAY_DOWN_ANG_SHOOT;
            return num <= Mathf.Abs(lay_DOWN_ANG_SHOOT) && Vector.CanShootToTarget(new ShootPointClass(target, 1f), vector, BotOwner.LookSensor.Mask, true);
        }
    }
}