using Newtonsoft.Json;
using SAIN.Attributes;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class GeneralSettings : SAINSettingsBase<GeneralSettings>, ISAINSettings
    {
        [Name("Bots Use Grenades")]
        public bool BotsUseGrenades = true;
        
        [Name("Bots Use Grenades Vs Other Bots")]
        [Description("Bots are not as careful with grenades as players, this will prevent accidental deaths fighting other bots.")]
        public bool BotVsBotGrenade = true;

        [Name("Bot Weight Effects")]
        [Description("Bots are properly affected by the weight of their equipment and loot. Requires raid restart for existing bots, as it applies on bot creation.")]
        public bool BotWeightEffects = true;

        [Name("Vanilla Bot Behavior Settings")]
        [Description("If a option here is set to ON, they will use vanilla logic, ALL Features will be disabled for these types, including personality, recoil, difficulty, and behavior.")]
        public VanillaBotSettings VanillaBots = new();

        public PerformanceSettings Performance = new();

        public AILimitSettings AILimit = new();

        public CoverSettings Cover = new();

        public DoorSettings Doors = new();

        public ExtractSettings Extract = new();

        public FlashlightSettings Flashlight = new();

        [Name("Looting Bots Integration")]
        [Description("Modify settings that relate to Looting Bots. Requires Looting Bots to be installed.")]
        public LootingBotsSettings LootingBots = new();

        public JokeSettings Jokes = new();

        public DebugSettings Debug = new();

        [Hidden]
        public LayerSettings Layers = new();

        public override void Init(List<ISAINSettings> list)
        {
            list.Add(this);
            list.Add(VanillaBots);
            list.Add(Performance);
            list.Add(AILimit);
            list.Add(Cover);
            list.Add(Doors);
            list.Add(Extract);
            list.Add(Flashlight);
            list.Add(LootingBots);
            list.Add(Jokes);
            list.Add(Layers);
            Debug.Init(list);
        }

        [JsonIgnore]
        [Hidden]
        public float SprintReachDistance = 1f;

        [JsonIgnore]
        [Hidden]
        public float BaseReachDistance = 0.5f;
    }

}