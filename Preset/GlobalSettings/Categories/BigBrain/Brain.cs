using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings.Categories
{
    public enum Brain
    {
        ArenaFighter,
        BossBully,
        BossGluhar,
        BossBoar,
        BossPartisan,
        Knight,
        BossKojaniy,
        BossSanitar,
        BossKolontay,
        Tagilla,
        BossTest,
        //BossZryachiy,
        Obdolbs,
        ExUsec,
        BigPipe,
        BirdEye,
        FollowerBully,
        FollowerGluharAssault,
        FollowerGluharProtect,
        FollowerGluharScout,
        FollowerKojaniy,
        FollowerSanitar,
        FollowerBoar,
        FollowerBoarClose1,
        FollowerBoarClose2,
        BossBoarSniper,
        FollowerKolontayAssault,
        FollowerKolontaySecurity,
        TagillaFollower,
        //Fl_Zraychiy,
        Gifter,
        Killa,
        Marksman,
        PMC,
        SectantPriest,
        SectantWarrior,
        CursAssault,
        Assault,
    }

    public static class AIBrains
    {
        public static readonly List<Brain> Scavs = new()
        {
            Brain.CursAssault,
            Brain.Assault,
        };

        public static readonly List<Brain> Goons = new()
        {
            Brain.Knight,
            Brain.BirdEye,
            Brain.BigPipe,
        };

        public static readonly List<Brain> Others = new()
        {
            Brain.Obdolbs,
        };

        public static readonly List<Brain> Bosses = new()
        {
            Brain.BossBully,
            Brain.BossGluhar,
            Brain.BossKojaniy,
            Brain.BossSanitar,
            Brain.Tagilla,
            Brain.BossTest,
            //Brain.BossZryachiy,
            Brain.Gifter,
            Brain.Killa,
            Brain.SectantPriest,
            Brain.BossBoar,
            Brain.BossKolontay,
            Brain.BossPartisan
        };

        public static readonly List<Brain> Followers = new()
        {
            Brain.BossBully,
            Brain.FollowerBully,
            Brain.FollowerGluharAssault,
            Brain.FollowerGluharProtect,
            Brain.FollowerGluharScout,
            Brain.FollowerKojaniy,
            Brain.FollowerSanitar,
            Brain.TagillaFollower,
            //Brain.Fl_Zraychiy,
            Brain.FollowerBoar,
            Brain.FollowerBoarClose1,
            Brain.FollowerBoarClose2,
            Brain.BossBoarSniper,
            Brain.FollowerKolontayAssault,
            Brain.FollowerKolontaySecurity,
        };
    }
}