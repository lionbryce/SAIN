using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SAIN.Components;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using static SAIN.Helpers.Shoot;

namespace SAIN.Patches.Shoot.RateOfFire
{
    public class FullAutoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ShootData), nameof(ShootData.method_6));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ____owner, ref float ___nextFingerUpTime)
        {
            if (SAINPlugin.IsBotExluded(____owner))
            {
                return true;
            }
            if (____owner.AimingManager.CurrentAiming == null)
            {
                return true;
            }

            Weapon weapon = ____owner.WeaponManager.CurrentWeapon;

            if (weapon.SelectedFireMode == Weapon.EFireMode.fullauto)
            {
                float distance = ____owner.AimingManager.CurrentAiming.LastDist2Target;
                float scaledDistance = FullAutoBurstLength(____owner, distance);

                ___nextFingerUpTime = scaledDistance + Time.time;

                return false;
            }

            ___nextFingerUpTime = 0.001f + Time.time;

            return true;
        }
    }

    public class SemiAutoPatch3 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass453), nameof(GClass453.method_0));
        }

        [PatchPostfix]
        public static void PatchPostfix(BotOwner ___botOwner_0, ref float __result)
        {
            if (SAINPlugin.IsBotExluded(___botOwner_0))
            {
                return;
            }
            if (SAINBotController.Instance.GetSAIN(___botOwner_0, out var component))
            {
                __result = component.Info.WeaponInfo.Firerate.SemiAutoROF();
            }
        }
    }

    public class SemiAutoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass453), nameof(GClass453.method_1));
        }

        [PatchPostfix]
        public static void PatchPostfix(BotOwner ___botOwner_0, ref float __result)
        {
            if (SAINPlugin.IsBotExluded(___botOwner_0))
            {
                return;
            }
            if (SAINBotController.Instance.GetSAIN(___botOwner_0, out var component))
            {
                __result = component.Info.WeaponInfo.Firerate.SemiAutoROF();
            }
        }
    }

    public class SemiAutoPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass453), nameof(GClass453.method_6));
        }

        [PatchPostfix]
        public static void PatchPostfix(BotOwner ___botOwner_0, ref float __result)
        {
            if (SAINPlugin.IsBotExluded(___botOwner_0))
            {
                return;
            }
            if (SAINBotController.Instance.GetSAIN(___botOwner_0, out var component))
            {
                __result = component.Info.WeaponInfo.Firerate.SemiAutoROF();
            }
        }
    }
}