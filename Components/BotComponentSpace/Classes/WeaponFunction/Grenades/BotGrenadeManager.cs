using UnityEngine;

namespace SAIN.SAINComponent.Classes.WeaponFunction
{
    public class BotGrenadeManager : BotBase, IBotClass
    {
        public ThrowWeapItemClass MyGrenade { get; set; }
        public Vector3? GrenadeDangerPoint => GrenadeReactionClass.GrenadeDangerPoint;

        public GrenadeThrowDecider GrenadeThrowDecider { get; }
        public GrenadeReactionClass GrenadeReactionClass { get; }

        public BotGrenadeManager(BotComponent sain) : base(sain)
        {
            GrenadeThrowDecider = new GrenadeThrowDecider(this);
            GrenadeReactionClass = new GrenadeReactionClass(this);
        }

        public void Init()
        {
            GrenadeThrowDecider.Init();
            GrenadeReactionClass.Init();
        }

        public void Update()
        {
            GrenadeThrowDecider.Update();
            GrenadeReactionClass.Update();
        }

        public void Dispose()
        {
            GrenadeThrowDecider.Dispose();
            GrenadeReactionClass.Dispose();
        }
    }
}