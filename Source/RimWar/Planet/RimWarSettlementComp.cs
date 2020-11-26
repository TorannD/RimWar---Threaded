﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using HarmonyLib;
using RimWar;
using RimWorld;
using RimWorld.Planet;
using FactionColonies;

namespace RimWar.Planet
{
    public class RimWarSettlementComp : WorldObjectComp
    {
        public bool isCapitol = false;
        private int rimwarPointsInt = 0;
        public int nextEventTick = 0;
        public int nextSettlementScan = 0;
        List<ConsolidatePoints> consolidatePoints;
        private int playerHeat = 0;
        public int PlayerHeat
        {
            get
            {
                return playerHeat;
            }
            set
            {
                playerHeat = Mathf.Clamp(value, 0, 10000);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.rimwarPointsInt, "rimwarPointsInt", 0, false);
            Scribe_Values.Look<int>(ref this.playerHeat, "playerHeat", 0, false);
            Scribe_Values.Look<int>(ref this.nextEventTick, "nextEventTick", 0, false);
            Scribe_Values.Look<int>(ref this.nextSettlementScan, "nextSettlementScan", 0, false);
            Scribe_Collections.Look<RimWorld.Planet.Settlement>(ref this.settlementsInRange, "settlementsInRange", LookMode.Reference, new object[0]);
            Scribe_Collections.Look<ConsolidatePoints>(ref this.consolidatePoints, "consolidatePoints", LookMode.Deep, new object[0]);
            Scribe_Values.Look<bool>(ref this.isCapitol, "isCapitol", false);
        }

        public List<ConsolidatePoints> SettlementPointGains
        {
            get
            {
                if (consolidatePoints == null)
                {
                    consolidatePoints = new List<ConsolidatePoints>();
                    consolidatePoints.Clear();
                }
                return consolidatePoints;
            }
            set
            {
                consolidatePoints = value;
            }
        }

        public RimWarData RWD
        {
            get
            {
                return WorldUtility.GetRimWarDataForFaction(this.parent.Faction);
            }
        }

        public int SettlementScanRange
        {
            get
            {
                float r = ((.4f * this.RimWarPoints) + 1000) / Options.Settings.Instance.settlementScanRangeDivider;
                return Mathf.RoundToInt(Mathf.Clamp(r, 10, Options.Settings.Instance.maxSettlementScanRange));
            }
        }

        //public override void PostDrawExtraSelectionOverlays()
        //{
        //    if(isCapitol)
        //    {
        //        Vector3 tileCenter = Find.WorldGrid.GetTileCenter(this.parent.Tile);
        //        Vector3 s = new Vector3(3f, 1f, 3f);
        //        Matrix4x4 matrix = default(Matrix4x4);
        //        matrix.SetTRS(tileCenter, Quaternion.identity, s);
        //        Graphics.DrawMesh(MeshPool.plane10, matrix, RimWarMatPool.Settlement_CapitolStar, WorldCameraManager.WorldLayer);
        //    }
        //    base.PostDrawExtraSelectionOverlays();
        //}

        public int RimWarPoints
        {
            get
            {
                if (this.RWD != null && this.parent != null)
                {
                    if (this.RWD.behavior == RimWarBehavior.Player)
                    {
                        Map map = null;
                        for (int i = 0; i < Verse.Find.Maps.Count; i++)
                        {
                            if (Verse.Find.Maps[i].Tile == this.parent.Tile)
                            {
                                map = Verse.Find.Maps[i];
                            }
                        }
                        if (map != null)
                        {
                            Options.SettingsRef settingsRef = new Options.SettingsRef();
                            if (settingsRef.storytellerBasedDifficulty)
                            {
                                return Mathf.RoundToInt(StorytellerUtility.DefaultThreatPointsNow(map) * 1.2f * WorldUtility.GetDifficultyMultiplierFromStoryteller());
                            }
                            return Mathf.RoundToInt(StorytellerUtility.DefaultThreatPointsNow(map) * settingsRef.rimwarDifficulty);
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    if (this.RWD.behavior == RimWarBehavior.Vassal)
                    {
                        FactionFC component = Find.World.GetComponent<FactionFC>();
                        if (component != null)
                        {
                            SettlementFC sfc = component.returnSettlementByLocation(this.parent.Tile, Find.World.info.name);
                            if (sfc != null)
                            {
                                this.rimwarPointsInt = Mathf.Clamp(this.rimwarPointsInt, 100, (sfc.settlementLevel * 500));
                            }
                        }
                    }
                }
                this.rimwarPointsInt = Mathf.Clamp(this.rimwarPointsInt, 100, 100000);
                return this.rimwarPointsInt;
            }
            set
            {
                this.rimwarPointsInt = Mathf.Max(0, value);
            }
        }        

        public int ReinforcementPoints
        {
            get
            {
                int pts = this.RimWarPoints - 1050;                
                if(this.parent.def.defName == "City_Faction")
                {
                    pts -= 1000;
                }
                else if(this.parent.def.defName == "City_Citadel")
                {
                    pts = -1;
                }
                else if(this.parent.def.defName == "City_Abandoned" || this.parent.def.defName == "City_Compromised" || this.parent.def.defName == "City_Ghost")
                {
                    pts = -1;
                }
                return pts;
            }
        }

        private List<RimWorld.Planet.Settlement> settlementsInRange;
        public List<RimWorld.Planet.Settlement> OtherSettlementsInRange
        {
            get
            {
                if (this.settlementsInRange == null)
                {
                    this.settlementsInRange = new List<RimWorld.Planet.Settlement>();
                    this.settlementsInRange.Clear();
                }
                if (this.settlementsInRange.Count == 0 || this.nextSettlementScan <= Find.TickManager.TicksGame)
                {
                    WorldComponent_PowerTracker.tasker.Register(() =>
                    {
                        List<Settlement> tmpSettlementsInRange = new List<Settlement>();
                        tmpSettlementsInRange.Clear();
                        Options.SettingsRef settingsRef = new Options.SettingsRef();
                        List<RimWorld.Planet.Settlement> scanSettlements = WorldUtility.GetRimWorldSettlementsInRange(this.parent.Tile, SettlementScanRange);
                        if (scanSettlements != null && scanSettlements.Count > 0)
                        {
                            for (int i = 0; i < scanSettlements.Count; i++)
                            {
                                if (scanSettlements[i] != this.parent)
                                {
                                    tmpSettlementsInRange.Add(scanSettlements[i]);
                                }
                            }
                        }
                        this.nextSettlementScan = Find.TickManager.TicksGame + settingsRef.settlementScanDelay;
                        this.settlementsInRange = tmpSettlementsInRange;
                        return null;
                    }, (context) =>
                    {
                    });
                    
                }
                return this.settlementsInRange;
            }
            set
            {
                this.settlementsInRange = value;
            }
        }

        public List<RimWorld.Planet.Settlement> NearbyHostileSettlements
        {
            get
            {
                List<RimWorld.Planet.Settlement> tmpSettlements = new List<RimWorld.Planet.Settlement>();
                tmpSettlements.Clear();
                if (OtherSettlementsInRange != null && settlementsInRange.Count > 0)
                {
                    for (int i = 0; i < settlementsInRange.Count; i++)
                    {
                        if (settlementsInRange[i] != null && settlementsInRange[i].Faction != null && settlementsInRange[i].Faction.HostileTo(this.parent.Faction))
                        {
                            tmpSettlements.Add(settlementsInRange[i]);
                        }
                    }
                }
                return tmpSettlements;
            }
        }

        public List<RimWorld.Planet.Settlement> NearbyFriendlySettlements
        {
            get
            {
                List<RimWorld.Planet.Settlement> tmpSettlements = new List<RimWorld.Planet.Settlement>();
                tmpSettlements.Clear();
                if (OtherSettlementsInRange != null && settlementsInRange.Count > 0)
                {
                    for (int i = 0; i < settlementsInRange.Count; i++)
                    {
                        if (settlementsInRange[i] != null && !settlementsInRange[i].Faction.HostileTo(this.parent.Faction))
                        {
                            tmpSettlements.Add(settlementsInRange[i]);
                        }
                    }
                }
                return tmpSettlements;
            }
        }

        public override void Initialize(WorldObjectCompProperties props)
        {
            base.Initialize(props);
            this.settlementsInRange = new List<RimWorld.Planet.Settlement>();
            this.settlementsInRange.Clear();
        }

        WorldObjectDef sendTypeDef = null;
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if(WorldUtility.GetRimWarDataForFaction(this.parent.Faction).AllianceFactions.Contains(Faction.OfPlayer) || WorldUtility.GetRimWarDataForFaction(this.parent.Faction).behavior == RimWarBehavior.Vassal)
            {
                int ptsToSend = 500;
                Command_Action command_SendTrader = new Command_Action();
                command_SendTrader.defaultLabel = "RW_SendTrader".Translate();
                command_SendTrader.defaultDesc = "RW_SendTraderDesc".Translate();
                command_SendTrader.icon = RimWarMatPool.Icon_Trader;
                if(this.RimWarPoints < ptsToSend)
                {
                    command_SendTrader.Disable("RW_NotEnoughPointsToSendUnit".Translate(this.RimWarPoints, ptsToSend, RimWarDefOf.RW_Trader.label));
                }
                command_SendTrader.action = delegate
                {
                    sendTypeDef = RimWarDefOf.RW_Trader;
                    StartChoosingRequestDestination();
                };
                yield return (Gizmo)command_SendTrader;

                Command_Action command_SendScout = new Command_Action();
                command_SendScout.defaultLabel = "RW_SendScout".Translate();
                command_SendScout.defaultDesc = "RW_SendScoutDesc".Translate();
                command_SendScout.icon = RimWarMatPool.Icon_Scout;
                ptsToSend = 800;
                if (this.RimWarPoints < ptsToSend)
                {
                    command_SendScout.Disable("RW_NotEnoughPointsToSendUnit".Translate(this.RimWarPoints, ptsToSend, RimWarDefOf.RW_Scout.label));
                }
                command_SendScout.action = delegate
                {
                    sendTypeDef = RimWarDefOf.RW_Scout;
                    StartChoosingRequestDestination();
                };
                yield return (Gizmo)command_SendScout;

                Command_Action command_SendWarband = new Command_Action();
                command_SendWarband.defaultLabel = "RW_SendWarband".Translate();
                command_SendWarband.defaultDesc = "RW_SendWarbandDesc".Translate();
                command_SendWarband.icon = RimWarMatPool.Icon_Warband;
                ptsToSend = 1000;
                if (this.RimWarPoints < ptsToSend)
                {
                    command_SendWarband.Disable("RW_NotEnoughPointsToSendUnit".Translate(this.RimWarPoints, ptsToSend, RimWarDefOf.RW_Warband.label));
                }
                command_SendWarband.action = delegate
                {
                    sendTypeDef = RimWarDefOf.RW_Warband;
                    StartChoosingRequestDestination();
                };
                yield return (Gizmo)command_SendWarband;

                Command_Action command_LaunchWarband = new Command_Action();
                command_LaunchWarband.defaultLabel = "RW_LaunchWarband".Translate();
                command_LaunchWarband.defaultDesc = "RW_LaunchWarbandDesc".Translate();
                command_LaunchWarband.icon = RimWarMatPool.Icon_LaunchWarband;
                ptsToSend = 1200;
                if(!RWD.CanLaunch)
                {
                    command_LaunchWarband.Disable("RW_FactionIncapableOfTech".Translate(this.parent.Faction.Name));
                }
                if (this.RimWarPoints < ptsToSend)
                {
                    command_LaunchWarband.Disable("RW_NotEnoughPointsToSendUnit".Translate(this.RimWarPoints, ptsToSend, RimWarDefOf.RW_LaunchedWarband.label));
                }
                command_LaunchWarband.action = delegate
                {
                    sendTypeDef = RimWarDefOf.RW_LaunchedWarband;
                    StartChoosingRequestDestination();
                };
                yield return (Gizmo)command_LaunchWarband;
            }
        }

        public void StartChoosingRequestDestination()
        {
            Find.WorldSelector.ClearSelection();
            int tile = this.parent.Tile;
            int maxRange = SettlementScanRange;
            Find.WorldTargeter.BeginTargeting_NewTemp(new Func<GlobalTargetInfo, bool>(ChooseWorldTarget), true, sendTypeDef.ExpandingIconTexture, false, delegate
            {
                GenDraw.DrawWorldRadiusRing(tile, maxRange);  //center, max launch distance
            }, (GlobalTargetInfo target) => TargetingLabelGetter(target, tile, maxRange));
        }

        private bool ChooseWorldTarget(GlobalTargetInfo target)
        {
            if (!target.IsValid)
            {
                Messages.Message("Invalid Tile", MessageTypeDefOf.RejectInput);
                return false;
            }
            if (Find.World.Impassable(target.Tile))
            {
                Messages.Message("Impassable Tile", MessageTypeDefOf.RejectInput);
                return false;
            }
            WorldObject wo = target.WorldObject;
            
            if (wo != null)
            {
                if(sendTypeDef == RimWarDefOf.RW_Trader)
                {
                    RimWorld.Planet.Settlement s = wo as Settlement;
                    if(s != null)
                    {
                        if (this.parent.Faction.HostileTo(s.Faction))
                        {
                            Messages.Message("Will not trade with hostile factions", MessageTypeDefOf.RejectInput);
                            return false;
                        }
                        target = s;
                        //send caravan
                        int relationsCost = -15;
                        int pointsCost = Mathf.RoundToInt(this.RimWarPoints * .3f);
                        if (this.RWD.behavior == RimWarBehavior.Vassal)
                        {
                            this.RimWarPoints -= pointsCost;
                        }
                        else
                        {
                            this.parent.Faction.TryAffectGoodwillWith(Faction.OfPlayer, relationsCost, true, true, "Requested action");
                        }
                        WorldUtility.CreateWarObjectOfType(new Trader(), pointsCost, this.RWD, this.parent as Settlement, this.parent.Tile, s, WorldObjectDefOf.Settlement);
                        return true;
                    }
                    else
                    {
                        Messages.Message("RW_DestinationSettlementOnly".Translate(), MessageTypeDefOf.RejectInput);
                        return false;
                    }
                }
                if (sendTypeDef == RimWarDefOf.RW_Scout)
                {
                    //if(wo is WarObject)
                    //{
                    //    if(!this.parent.Faction.HostileTo(wo.Faction))
                    //    {
                    //        Messages.Message("Will only scout hostile units", MessageTypeDefOf.RejectInput);
                    //        return false;
                    //    }
                    //}
                    //else
                    //{
                    //    Settlement s = wo as Settlement;
                    //    if(s == null)
                    //    {
                    //        Messages.Message("Invalid target", MessageTypeDefOf.RejectInput);
                    //        return false;
                    //    }
                    //}
                    target = wo;
                    //send caravan
                    int relationsCost = -20;
                    int pointsCost = Mathf.RoundToInt(this.RimWarPoints * .45f);
                    if (this.RWD.behavior == RimWarBehavior.Vassal)
                    {
                        this.RimWarPoints -= pointsCost;
                    }
                    else
                    {
                        this.parent.Faction.TryAffectGoodwillWith(Faction.OfPlayer, relationsCost, true, true, "Requested action");
                    }
                    WorldUtility.CreateWarObjectOfType(new Scout(), pointsCost, this.RWD, this.parent as Settlement, this.parent.Tile, wo, wo.def);
                    return true;
                }
                if (sendTypeDef == RimWarDefOf.RW_Warband)
                {
                    target = wo;
                    int relationsCost = -25;
                    int pointsCost = Mathf.RoundToInt(this.RimWarPoints * .6f);
                    if (this.RWD.behavior == RimWarBehavior.Vassal)
                    {
                        this.RimWarPoints -= pointsCost;
                    }
                    else
                    {
                        this.parent.Faction.TryAffectGoodwillWith(Faction.OfPlayer, relationsCost, true, true, "Requested action");
                    }
                    WorldUtility.CreateWarObjectOfType(new Warband(), pointsCost, this.RWD, this.parent as Settlement, this.parent.Tile, wo, wo.def);
                    return true;
                }
                if (sendTypeDef == RimWarDefOf.RW_LaunchedWarband)
                {
                    target = wo;
                    int relationsCost = -25;
                    int pointsCost = Mathf.RoundToInt(this.RimWarPoints * .6f);
                    if (this.RWD.behavior == RimWarBehavior.Vassal)
                    {
                        this.RimWarPoints -= pointsCost;
                    }
                    else
                    {
                        this.parent.Faction.TryAffectGoodwillWith(Faction.OfPlayer, relationsCost, true, true, "Requested action");
                    }
                    WorldUtility.CreateLaunchedWarband(pointsCost, this.RWD, this.parent as Settlement, this.parent.Tile, wo, wo.def);
                    return true;
                }
            }
            return false;
        }

        public string TargetingLabelGetter(GlobalTargetInfo target, int tile, int maxLaunchDistance)
        {
            if (!target.IsValid)
            {
                return null;
            }
            if (Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile) > maxLaunchDistance)
            {
                GUI.color = ColoredText.RedReadable;
                return "RW_SendCannotReach".Translate();
            }
            WorldObject wo = target.WorldObject;
            if (wo == null && sendTypeDef != RimWarDefOf.RW_Settler)
            {
                GUI.color = ColoredText.RedReadable;
                return "RW_InvalidTarget".Translate();
            }
            if (sendTypeDef == RimWarDefOf.RW_Trader)
            {                
                Settlement s = wo as Settlement;
                if(s == null)
                {
                    GUI.color = ColoredText.RedReadable;
                    return "RW_DestinationSettlementOnly".Translate();
                }
                if(this.parent.Faction.HostileTo(s.Faction))
                {
                    GUI.color = ColoredText.RedReadable;
                    return "RW_DestinationHostile".Translate();
                }
            }

            return "Send " + sendTypeDef.label;
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            if (!Options.Settings.Instance.forceRandomObject)
            {
                GenDraw.DrawWorldRadiusRing(this.parent.Tile, SettlementScanRange);
                //float averageTileSize = Find.WorldGrid.averageTileSize;
                //float transitionPct = ExpandableWorldObjectsUtility.TransitionPct;
                //if (this.parent.def.expandingIcon && transitionPct > 0f)
                //{
                //    float num = 1f - transitionPct;
                //    WorldRendererUtility.DrawQuadTangentialToPlanet(this.parent.DrawPos, 0.7f * averageTileSize, 0.015f, RimWarMatPool.Material_Exclamation_Green);
                //}
            }
            base.PostDrawExtraSelectionOverlays();
        }
    }
}