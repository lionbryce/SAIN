using BepInEx;
using BepInEx.Configuration;
using EFT;
using SAIN.Editor;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using UnityEngine;
using static SAIN.AssemblyInfoClass;

// advanced insurance mod
// track insured items, override default spt behavior
// if an item is dropped, track if the position it was dropped at is on the navmesh or not, if its in a bush or not, maybe figure out of way to detect "hiding spots"?
// if an item is dropped in a non-hidden area, track how long it was there before the player left the area, based return percentage off of that.
// if a player is killed, track the gear status of the person who killed them, base return percentage off of that.
// also could track backpack size/space available, maybe make it require looting bots for simpler code?
// if a player dies to landmines, ect, have a 100% return rate
// lower return percentage when killed by scavs or PMCs, very high from bosses, followers, rogues, ect

namespace SAIN
{
    [BepInPlugin(SAINGUID, SAINName, SAINVersion)]
    [BepInDependency(BigBrainGUID, BigBrainVersion)]
    //[BepInDependency(SPTGUID, SPTVersion)]
    [BepInProcess(EscapeFromTarkov)]
    [BepInIncompatibility("com.dvize.BushNoESP")]
    [BepInIncompatibility("com.dvize.NoGrenadeESP")]
    public class SAINPlugin : BaseUnityPlugin
    {
        public static DebugSettings DebugSettings => LoadedPreset.GlobalSettings.General.Debug;
        public static bool DebugMode => true;
        public static bool ProfilingMode => DebugSettings.Logs.GlobalProfilingToggle;
        public static bool DrawDebugGizmos => DebugSettings.Gizmos.DrawDebugGizmos;
        public static PresetEditorDefaults EditorDefaults => PresetHandler.EditorDefaults;

        public static ECombatDecision ForceSoloDecision = ECombatDecision.None;

        public static ESquadDecision ForceSquadDecision = ESquadDecision.None;

        public static ESelfDecision ForceSelfDecision = ESelfDecision.None;

        public void Awake()
        {
            /*
            if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception("Invalid EFT Version");
            }
            */

            PresetHandler.Init();
            BindConfigs();
            InitPatches();
            BigBrainHandler.Init();
            Vector.Init();
        }

        private void BindConfigs()
        {
            string category = "SAIN Editor";
            OpenEditorButton = Config.Bind(category, "Open Editor", false, "Opens the Editor on press");
            OpenEditorConfigEntry = Config.Bind(category, "Open Editor Shortcut", new KeyboardShortcut(KeyCode.F6), "The keyboard shortcut that toggles editor");
        }

        public static ConfigEntry<bool> OpenEditorButton { get; private set; }

        public static ConfigEntry<KeyboardShortcut> OpenEditorConfigEntry { get; private set; }

        private List<ModulePatch> SainPatches => [
            new Patches.Generic.StopRefillMagsPatch(),
            new Patches.Generic.SetEnvironmentPatch(),
            new Patches.Generic.SetPanicPointPatch(),
            new Patches.Generic.AddPointToSearchPatch(),
            new Patches.Generic.TurnDamnLightOffPatch(),
            new Patches.Generic.GrenadeThrownActionPatch(),
            new Patches.Generic.GrenadeExplosionActionPatch(),
            new Patches.Generic.ShallKnowEnemyPatch(),
            new Patches.Generic.ShallKnowEnemyLatePatch(),
            new Patches.Generic.HaveSeenEnemyPatch(),
            new Patches.Generic.AllowRequestPatch(),
            new Patches.Generic.FindRequestForMePatch(),

            new Patches.Generic.Fixes.StopSetToNavMeshPatch(),
            new Patches.Generic.Fixes.FightShallReloadFixPatch(),
            new Patches.Generic.Fixes.EnableVaultPatch(),
            new Patches.Generic.Fixes.BotMemoryAddEnemyPatch(),
            new Patches.Generic.Fixes.BotGroupAddEnemyPatch(),
            new Patches.Generic.Fixes.FixItemTakerPatch(),
            new Patches.Generic.Fixes.FixItemTakerPatch2(),
            new Patches.Generic.Fixes.RotateClampPatch(),

            new Patches.Movement.EncumberedPatch(),
            new Patches.Movement.DoorOpenerPatch(),
            new Patches.Movement.DoorDisabledPatch(),
            new Patches.Movement.CrawlPatch(),
            new Patches.Movement.CrawlPatch2(),
            new Patches.Movement.PoseStaminaPatch(),
            new Patches.Movement.AimStaminaPatch(),
            new Patches.Movement.GlobalShootSettingsPatch(),
            new Patches.Movement.GlobalLookPatch(),

            new Patches.Hearing.TryPlayShootSoundPatch(),
            new Patches.Hearing.OnMakingShotPatch(),
            new Patches.Hearing.HearingSensorPatch(),

            new Patches.Hearing.GrenadeCollisionPatch(),
            new Patches.Hearing.GrenadeCollisionPatch2(),

            new Patches.Hearing.ToggleSoundPatch(),
            new Patches.Hearing.SpawnInHandsSoundPatch(),
            new Patches.Hearing.PlaySwitchHeadlightSoundPatch(),
            new Patches.Hearing.BulletImpactPatch(),
            new Patches.Hearing.TreeSoundPatch(),
            new Patches.Hearing.DoorBreachSoundPatch(),
            new Patches.Hearing.DoorOpenSoundPatch(),
            new Patches.Hearing.FootstepSoundPatch(),
            new Patches.Hearing.SprintSoundPatch(),
            new Patches.Hearing.GenericMovementSoundPatch(),
            new Patches.Hearing.JumpSoundPatch(),
            new Patches.Hearing.DryShotPatch(),
            new Patches.Hearing.ProneSoundPatch(),
            new Patches.Hearing.SoundClipNameCheckerPatch(),
            new Patches.Hearing.SoundClipNameCheckerPatch2(),
            new Patches.Hearing.AimSoundPatch(),
            new Patches.Hearing.LootingSoundPatch(),
            new Patches.Hearing.SetInHandsGrenadePatch(),
            new Patches.Hearing.SetInHandsFoodPatch(),
            new Patches.Hearing.SetInHandsMedsPatch(),

            new Patches.Talk.JumpPainPatch(),
            new Patches.Talk.PlayerHurtPatch(),
            new Patches.Talk.PlayerTalkPatch(),
            new Patches.Talk.BotTalkPatch(),
            new Patches.Talk.BotTalkManualUpdatePatch(),

            new Patches.Vision.DisableLookUpdatePatch(),
            new Patches.Vision.UpdateLightEnablePatch(),
            new Patches.Vision.UpdateLightEnablePatch2(),
            new Patches.Vision.ToggleNightVisionPatch(),
            new Patches.Vision.SetPartPriorityPatch(),
            new Patches.Vision.GlobalLookSettingsPatch(),
            new Patches.Vision.WeatherTimeVisibleDistancePatch(),
            new Patches.Vision.NoAIESPPatch(),
            new Patches.Vision.BotLightTurnOnPatch(),
            new Patches.Vision.VisionSpeedPatch(),
            new Patches.Vision.WeatherVisionPatch(),
            new Patches.Vision.VisionDistancePatch(),
            new Patches.Vision.CheckFlashlightPatch(),

            new Patches.Shoot.Aim.DoHitAffectPatch(),
            new Patches.Shoot.Aim.HitAffectApplyPatch(),
            new Patches.Shoot.Aim.PlayerHitReactionDisablePatch(),

            new Patches.Shoot.Aim.SetAimStatusPatch(),
            new Patches.Shoot.Aim.AimOffsetPatch(),
            new Patches.Shoot.Aim.AimTimePatch(),
            new Patches.Shoot.Aim.ForceNoHeadAimPatch(),
            new Patches.Shoot.Aim.AimRotateSpeedPatch(),

            new Patches.Shoot.Grenades.ResetGrenadePatch(),
            new Patches.Shoot.Grenades.SetGrenadePatch(),

            new Patches.Shoot.RateOfFire.FullAutoPatch(),
            new Patches.Shoot.RateOfFire.SemiAutoPatch(),
            new Patches.Shoot.RateOfFire.SemiAutoPatch2(),
            new Patches.Shoot.RateOfFire.SemiAutoPatch3(),

            new Patches.Components.AddBotComponentPatch(),
            new Patches.Components.AddGameWorldPatch(),
            new Patches.Components.GetBotController(),
            new Patches.Components.GetBotSpawner(),
        ];


        private void InitPatches()
        {
            foreach (var patch in SainPatches)
            {
                patch.Enable();
            }
        }

        public static SAINPresetClass LoadedPreset => PresetHandler.LoadedPreset;

        public void Update()
        {
            ModDetection.Update();
            SAINEditor.Update();
        }

        public void Start() => SAINEditor.Init();

        public void LateUpdate() => SAINEditor.LateUpdate();

        public void OnGUI() => SAINEditor.OnGUI();

        public static bool IsBotExluded(BotOwner botOwner) => SAINEnableClass.IsSAINDisabledForBot(botOwner);
    }
}
