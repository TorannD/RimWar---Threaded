using System;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using System.Linq;

namespace RimWar.Planet
{
    public class TransportPodsArrivalAction_Shuttle_ReinforceSettlement : TransportersArrivalAction_TransportShip
    {
        private Settlement settlement;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref settlement, "settlement");
        }

        public TransportPodsArrivalAction_Shuttle_ReinforceSettlement()
        {
        }

        public TransportPodsArrivalAction_Shuttle_ReinforceSettlement(Settlement _settlement, MapParent _mapParent)
        {
            this.settlement = _settlement;
            this.mapParent = _mapParent;
        }

        public override void Arrived(List<ActiveTransporterInfo> pods, PlanetTile tile)
        {
            //Thing lookTarget = TransportPodsArrivalActionUtility.GetLookTarget(pods);
            if (transportShip == null || transportShip.Disposed)
            {
                Log.Error("Trying to arrive in a null or disposed transport ship.");
                return;
            }
            bool flag_hasMap = !this.settlement.HasMap;
            Map orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(this.settlement.Tile, null);
            if (!cell.IsValid)
            {
                cell = DropCellFinder.GetBestShuttleLandingSpot(orGenerateMap, Faction.OfPlayer);
            }
            LookTargets lookTargets = new LookTargets(cell, orGenerateMap);
            if (!cell.IsValid)
            {
                Log.Error("Could not find cell for transport ship arrival.");
                return;
            }
            Settlement settlement;
            if ((settlement = (orGenerateMap.Parent as Settlement)) != null && settlement.Faction != Faction.OfPlayer)
            {
                TaggedString letterLabel = "LetterLabelCaravanEnteredEnemyBase".Translate();
                TaggedString letterText = "LetterTransportPodsLandedInEnemyBase".Translate(settlement.Label).CapitalizeFirst();
                //SettlementUtility.AffectRelationsOnAttacked_NewTmp(sbs, ref letterText);
                if (flag_hasMap)
                {
                    Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
                    PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(orGenerateMap.mapPawns.AllPawns, ref letterLabel, ref letterText, "LetterRelatedPawnsInMapWherePlayerLanded".Translate(Faction.OfPlayer.def.pawnsPlural), informEvenIfSeenBefore: true);
                }
                Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.NeutralEvent, lookTargets);
            }
            for (int i = 0; i < pods.Count; i++)
            {
                transportShip.TransporterComp.innerContainer.TryAddRangeOrTransfer(pods[i].innerContainer, canMergeWithExistingStacks: true, destroyLeftover: true);
            }
            transportShip.ArriveAt(cell, mapParent);
            //foreach (ActiveDropPodInfo pod in pods)
            //{

            //    pod.missionShuttleHome = missionShuttleHome;
            //    pod.missionShuttleTarget = missionShuttleTarget;
            //    pod.sendAwayIfQuestFinished = sendAwayIfQuestFinished;
            //    pod.questTags = questTags;
            //}
            //PawnsArrivalModeDefOf.Shuttle.Worker.TravelingTransportPodsArrived(pods, orGenerateMap);
            Messages.Message("MessageShuttleArrived".Translate(), lookTargets, MessageTypeDefOf.TaskCompletion);
            RimWarSettlementComp rwsc = settlement.GetComponent<RimWarSettlementComp>();
            if (rwsc != null && rwsc.UnderAttack)
            {
                IncidentUtility.GenerateSettlementAttackers(rwsc, orGenerateMap);
            }
        }
    }
}
