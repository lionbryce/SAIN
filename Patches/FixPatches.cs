using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace SAIN.Patches.Generic.Fixes
{
    internal class EnableVaultPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.InitVaultingComponent));
        }

        [PatchPrefix]
        public static void Patch(Player __instance, ref bool aiControlled)
        {
            if (__instance.UsedSimplifiedSkeleton)
                return;

            aiControlled = false;
        }
    }

    internal class FightShallReloadFixPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotReload), nameof(BotReload.FightShallReload));
        }

        [PatchPrefix]
        public static bool Patch(BotOwner ___botOwner_0, ref bool __result)
        {
            if (SAINPlugin.IsBotExluded(___botOwner_0))
            {
                return true;
            }
            if (___botOwner_0.Medecine?.Using == true)
            {
                __result = false;
                return false;
            }
            if (!___botOwner_0.WeaponManager.IsWeaponReady)
            {
                __result = false;
                return false;
            }
            if (___botOwner_0.WeaponManager.Malfunctions.HaveMalfunction() &&
                ___botOwner_0.WeaponManager.Malfunctions.MalfunctionType() != Weapon.EMalfunctionState.Misfire)
            {
                __result = false;
                return false;
            }
            __result = true;
            return false;
        }
    }

    internal class FixItemTakerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotItemTaker), nameof(BotItemTaker.method_12));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0)
        {
            return GenericHelpers.CheckNotNull(___botOwner_0);
        }
    }

    internal class FixItemTakerPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotItemTaker), nameof(BotItemTaker.RefreshClosestItems));
        }

        [PatchPrefix]
        public static bool PatchPrefix(BotOwner ___botOwner_0)
        {
            return GenericHelpers.CheckNotNull(___botOwner_0);
        }
    }

    internal class RotateClampPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.Rotate));
        }

        [PatchPrefix]
        public static void PatchPrefix(ref Player __instance, ref bool ignoreClamp)
        {
            if (__instance?.IsAI == true
                && __instance.IsSprintEnabled)
            {
                ignoreClamp = true;
            }
        }
    }

    internal class BotGroupAddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsGroup), nameof(BotsGroup.AddEnemy));
        }

        [PatchPrefix]
        public static bool PatchPrefix(IPlayer person)
        {
            if (person == null || person.IsAI && person.AIData?.BotOwner?.GetPlayer == null)
            {
                return false;
            }

            return true;
        }
    }

    internal class BotMemoryAddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMemoryClass), nameof(BotMemoryClass.AddEnemy));
        }

        [PatchPrefix]
        public static bool PatchPrefix(IPlayer enemy)
        {
            if (enemy == null || enemy.IsAI && enemy.AIData?.BotOwner?.GetPlayer == null)
            {
                return false;
            }

            return true;
        }
    }

    public class StopSetToNavMeshPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMover), nameof(BotMover.method_9));
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref BotOwner ___botOwner_0)
        {
            return SAINPlugin.IsBotExluded(___botOwner_0);
        }
    }
}