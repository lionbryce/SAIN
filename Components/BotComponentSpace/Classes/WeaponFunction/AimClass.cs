using EFT;
using HarmonyLib;
using SAIN.Preset;
using SAIN.Preset.BotSettings.SAINSettings.Categories;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class AimClass(BotComponent sain) : BotBase(sain), IBotClass
    {
        public event Action<bool> OnAimAllowedOrBlocked;

        public bool CanAim { get; private set; }

        public float LastAimTime { get; set; }

        public AimStatus AimStatus
        {
            get
            {

                if (BotOwner.AimingManager.CurrentAiming != null && BotOwner.AimingManager.CurrentAiming is BotAimingClass aimClass)
                {
                    var status = aimClass.aimStatus_0;

                    //if (status != AimStatus.NoTarget &&
                    //    Bot.Enemy?.IsVisible == false &&
                    //    Bot.LastEnemy?.IsVisible == false)
                    //{
                    //    return AimStatus.NoTarget;
                    //}
                    return status;
                }
                else
                {
                    return AimStatus.NoTarget;
                }
            }
        }

        public void Init()
        {
        }

        public void Update()
        {
            checkCanAim();
            checkLoseTarget();
        }

        public void LateUpdate()
        {
        }

        private void checkCanAim()
        {
            bool couldAim = CanAim;
            CanAim = canAim();
            if (couldAim != CanAim)
            {
                OnAimAllowedOrBlocked?.Invoke(CanAim);
            }
        }

        private bool canAim()
        {
            var aimData = BotOwner.AimingManager.CurrentAiming;
            if (aimData == null)
            {
                //return false;
            }
            if (Player.IsSprintEnabled)
            {
                return false;
            }
            if (BotOwner.WeaponManager.Reload.Reloading)
            {
                //return false;
            }
            if (!Bot.HasEnemy)
            {
                //return false;
            }
            return true;
        }

        private void checkLoseTarget()
        {
            if (!CanAim)
            {
                BotOwner.AimingManager.CurrentAiming?.LoseTarget();
                return;
            }
        }

        public void Dispose()
        {
        }
    }
}