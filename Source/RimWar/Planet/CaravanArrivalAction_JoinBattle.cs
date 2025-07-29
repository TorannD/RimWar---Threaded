using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using System;

namespace RimWar.Planet
{
    public class CaravanArrivalAction_JoinBattle : CaravanArrivalAction
    {
        private BattleSite bs;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref bs, "bs");
        }

        public override string Label => "RW_JoinBattle".Translate();

        public override string ReportString => "RW_CaravanJoiningBattle".Translate();

        public CaravanArrivalAction_JoinBattle()
        {
        }

        public CaravanArrivalAction_JoinBattle(BattleSite _bs)
        {
            this.bs = _bs;
        }

        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(caravan, destinationTile);
            if (!(bool)floatMenuAcceptanceReport)
            {
                return floatMenuAcceptanceReport;
            }
            if (bs != null && bs.Tile != destinationTile)
            {
                return false;
            }
            return CanAttack(caravan, bs);
        }

        public static FloatMenuAcceptanceReport CanAttack(Caravan caravan, BattleSite bs)
        {
            if (bs == null || !bs.Spawned || !bs.Attackable)
            {
                return false;
            }
            if (bs.EnterCooldownBlocksEntering())
            {
                return FloatMenuAcceptanceReport.WithFailMessage("MessageEnterCooldownBlocksEntering".Translate(bs.EnterCooldownTicksLeft().ToStringTicksToPeriod()));
            }
            return true;
        }

        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, BattleSite bs)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions(() => CanAttack(caravan, bs), () => new CaravanArrivalAction_JoinBattle(bs), "RW_JoinBattle".Translate(), caravan, bs.Tile, bs);
        }

        public override void Arrived(Caravan caravan)
        {
            IncidentUtility.AttackBattleSite(caravan, bs);
        }
    }
}
