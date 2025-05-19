using EFT;
using SAIN.Helpers;
using SAIN.Models.Enums;
using SAIN.Preset.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class BlindFireController : BotBase, IBotClass
    {
        public BlindFireController(BotComponent sain) : base(sain)
        {
        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        private bool CheckAllowBlindFire()
        {
            if (!Bot.SAINLayersActive ||
                !BotOwner.WeaponManager.IsReady ||
                !BotOwner.WeaponManager.HaveBullets ||
                Bot.Player.IsSprintEnabled ||
                Bot.Cover.CoverInUse == null)
            {
                return false;
            }

            Enemy enemy = Bot.Enemy;
            if (enemy == null ||
                !enemy.Seen ||
                enemy.TimeSinceSeen > 30f)
            {
                return false;
            }

            if (enemy.IsVisible && enemy.CanShoot)
            {
                return false;
            }

            if (GlobalSettingsClass.Instance.General.AILimit.LimitAIvsAIGlobal
                && enemy.IsAI
                && Bot.CurrentAILimit != AILimitSetting.None)
            {
                return false;
            }

            if (!Bot.ManualShoot.CanShoot())
            {
                return false;
            }

            return true;
        }

        public void Update()
        {
            if (_nextBlindFireCheck > Time.time)
            {
                if (_blindFire != 0)
                {
                    TryShoot();
                }
                return;
            }
            _nextBlindFireCheck = Time.time + 0.2f;

            if (!CheckAllowBlindFire())
            {
                ResetBlindFire();
                return;
            }

            Vector3? lastKnownPos = Bot.Enemy.KnownPlaces.LastKnownPosition;
            if (lastKnownPos == null)
            {
                ResetBlindFire();
                _changeBlindFireTime = Time.time + 0.5f;
                return;
            }

            Vector3? targetPos = Bot.Steering.FindLastKnownTarget(Bot.Enemy);
            if (targetPos == null || (lastKnownPos.Value - targetPos.Value).sqrMagnitude > 10f)
            {
                ResetBlindFire();
                _changeBlindFireTime = Time.time + 0.5f;
                return;
            }

            int lastBlindFire = _blindFire;
            _blindFire = checkBlindFire(targetPos.Value);

            if (_blindFire == 0)
            {
                ResetBlindFire();
                _changeBlindFireTime = Time.time + 0.5f;
                return;
            }

            bool noLastBlindFire = lastBlindFire == 0;
            if (noLastBlindFire || _changeBlindFireTime < Time.time)
            {
                _changeBlindFireTime = Time.time + 1f;
                SetBlindFire(_blindFire);
            }
            if (noLastBlindFire || _nextUpdateAimTargetTime < Time.time || BlindFireTargetPos == Vector3.zero)
            {
                _nextUpdateAimTargetTime = Time.time + 1.5f;
                Vector3 start = Bot.Position;
                Vector3 blindFireDirection = Vector.Rotate(targetPos.Value - start, Vector.RandomRange(3), Vector.RandomRange(3), Vector.RandomRange(3));
                BlindFireTargetPos = blindFireDirection + start;
            }
            TryShoot();
        }

        private void TryShoot()
        {
            if (Bot.ManualShoot.TryShoot(true, BlindFireTargetPos, false, EShootReason.Blindfire))
            {
                _manualShooting = true;
            }
        }

        private float _nextUpdateAimTargetTime;
        private float _nextBlindFireCheck;
        private int _blindFire;

        private bool _manualShooting;

        public void Dispose()
        {
        }

        public void ResetBlindFire()
        {
            if (ActiveBlindFireSetting != 0)
            {
                _blindFire = 0;
                Player.MovementContext.SetBlindFire(0);
            }
            if (_blindFire != 0)
            {
                _blindFire = 0;
            }
            if (_manualShooting)
            {
                _manualShooting = false;
                Bot.ManualShoot.TryShoot(false, Vector3.zero);
            }
            if (BlindFireTargetPos != Vector3.zero)
            {
                BlindFireTargetPos = Vector3.zero;
            }
        }

        private Vector3 BlindFireTargetPos;

        public bool BlindFireActive => ActiveBlindFireSetting != 0;

        public int ActiveBlindFireSetting => Player.MovementContext.BlindFire;

        private float _changeBlindFireTime = 0f;

        private int checkBlindFire(Vector3 targetPos)
        {
            LayerMask mask = LayerMaskClass.HighPolyWithTerrainMask;
            Vector3 firePort = Bot.Transform.WeaponFirePort;
            Vector3 direction = targetPos - firePort;

            if (Physics.Raycast(firePort, direction, 5f, mask))
            {
                // Overhead blindfire
                firePort = Bot.Transform.HeadPosition + Vector3.up * 0.15f;
                if (!Vector.Raycast(firePort, targetPos, mask))
                {
                    return 1;
                }

                Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);
                Vector3 SideShoot = rotation * direction.normalized * 0.2f;
                firePort += SideShoot;
                direction = targetPos - firePort;
                if (!Physics.Raycast(firePort, direction, direction.magnitude, mask))
                {
                    return -1;
                }
            }
            return 0;
        }

        public void SetBlindFire(int value)
        {
            if (ActiveBlindFireSetting != value)
            {
                Player.MovementContext.SetBlindFire(value);
            }
        }
    }
}