﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace RimWar.Planet
{
    [StaticConstructorOnStartup]
    public class WarObject : WorldObject
    {
        private int uniqueId = -1;
        private string nameInt;
        private int warPointsInt = -1;

        public WarObject_PathFollower pather;
        public WarObject_GotoMoteRenderer gotoMote;
        public WarObject_Tweener tweener;

        private Material cachedMat;

        private bool movesAtNight = false;
        public bool launched = false;
        private bool cachedImmobilized;
        private int cachedImmobilizedForTicks = -99999;
        private const int ImmobilizedCacheDuration = 60;
        public bool interactable = true;

        private RimWorld.Planet.Settlement parentSettlement = null;
        private int parentSettlementTile = -1;
        private WorldObject targetWorldObject = null;
        private int destinationTile = -1;
        public int nextMoveTickIncrement = 0;

        public bool canReachDestination = true;
        public bool playerNotified = false;

        private bool useDestinationTile = false;
        public virtual bool UseDestinationTile
        {
            get
            {
                return useDestinationTile;
            }
        }

        public virtual int NextMoveTickIncrement
        {
            get
            {
                Options.SettingsRef settingsRef = new Options.SettingsRef();
                this.nextMoveTickIncrement = (int)settingsRef.woEventFrequency;
                return nextMoveTickIncrement;
            }
        }

        private int nextMoveTick;
        public virtual int NextMoveTick
        {
            get
            {
                return nextMoveTick;
            }
            set
            {
                nextMoveTick = value;
            }
        }

        public virtual int NextSearchTickIncrement
        {
            get
            {
                return Rand.Range(180, 300);
            }
        }

        private int nextSearchTick;
        public virtual int NextSearchTick
        {
            get
            {
                return nextSearchTick;
            }
            set
            {
                nextSearchTick = value;
            }
        }

        public virtual float ScanRange
        {
            get
            {
                return 1f;
            }
        }

        public virtual float DetectionModifier
        {
            get
            {
                return 1f;
            }
        }

        public virtual float MovementModifier
        {
            get
            {
                return 1f;
            }
        }

        public bool CaravanDetected(Caravan car)
        {
            float vis = car.Visibility * (1f / Find.WorldGrid.ApproxDistanceInTiles(car.Tile, this.Tile));
            bool det = Rand.Chance(vis * this.DetectionModifier);
            return det;
        }

        private static readonly Color WarObjectDefaultColor = new Color(1f, 1f, 1f);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref uniqueId, "uniqueId", 0);
            Scribe_Values.Look(ref nameInt, "name");
            Scribe_Values.Look<bool>(ref this.movesAtNight, "movesAtNight", false, false);
            Scribe_Values.Look<bool>(ref this.playerNotified, "playerNotified", false, false);
            Scribe_Values.Look<int>(ref this.warPointsInt, "warPointsInt", -1, false);
            Scribe_Values.Look<int>(ref this.parentSettlementTile, "parentSettlementTile", -1, false);
            Scribe_Values.Look<int>(ref this.destinationTile, "destinationTile", -1, false);
            Scribe_Deep.Look(ref pather, "pather", this);
            Scribe_References.Look<RimWorld.Planet.Settlement>(ref this.parentSettlement, "parentSettlement");
            Scribe_References.Look<WorldObject>(ref this.targetWorldObject, "targetWorldObject");
        }

        public RimWarSettlementComp WarSettlementComp
        {
            get
            {
                return this.ParentSettlement.GetComponent<RimWarSettlementComp>();
            }
        }

        public RimWorld.Planet.Settlement ParentSettlement
        {
            get
            {
                if (this.parentSettlement != null)
                {
                    if (this.parentSettlement.Faction != this.Faction || this.parentSettlement.Destroyed)
                    {
                        this.parentSettlementTile = -1;
                        this.parentSettlement = null;
                        FindParentSettlement();
                    }
                }
                else
                {
                    if (this.parentSettlementTile != -1)
                    {
                        RimWorld.Planet.Settlement wo = Find.WorldObjects.SettlementAt(this.parentSettlementTile);
                        if (wo != null && wo.Faction == this.Faction)
                        {
                            this.parentSettlement = wo;
                            if (this.parentSettlement == null || this.parentSettlement.Destroyed)
                            {
                                this.parentSettlementTile = -1;
                            }
                        }
                    }
                    if (this.parentSettlement == null)
                    {
                        FindParentSettlement();
                    }
                }
                return this.parentSettlement;
            }
            set
            {
                this.parentSettlement = value;
            }
        }

        public WorldObject DestinationTarget
        {
            get
            {
                if (targetWorldObject != null && targetWorldObject.Destroyed)
                {
                    targetWorldObject = null;
                }
                return this.targetWorldObject;
            }
            set
            {
                this.targetWorldObject = value;
            }
        }

        public int DestinationTile
        {
            get
            {
                return this.destinationTile;
            }
            set
            {
                this.destinationTile = value;
            }
        }

        public virtual int RimWarPoints
        {
            get
            {
                this.warPointsInt = Mathf.Clamp(warPointsInt, 50, 100000);
                return this.warPointsInt;
            }
            set
            {
                this.warPointsInt = Mathf.Max(0, value);
            }
        }

        public override Material Material
        {
            get
            {
                if (cachedMat == null)
                {
                    cachedMat = MaterialPool.MatFrom(color: (base.Faction == null) ? Color.white : ((!base.Faction.IsPlayer) ? base.Faction.Color : WarObjectDefaultColor), texPath: def.texture, shader: ShaderDatabase.WorldOverlayTransparentLit, renderQueue: WorldMaterials.DynamicObjectRenderQueue);
                }
                return cachedMat;
            }
        }

        public virtual bool MovesAtNight
        {
            get
            {
                return movesAtNight;
            }
            set
            {
                movesAtNight = value;
            }
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            gotoMote.RenderMote();
        }

        public string Name
        {
            get
            {
                return nameInt;
            }
            set
            {
                nameInt = value;
            }
        }

        public override bool HasName => !nameInt.NullOrEmpty();

        public override string Label
        {
            get
            {
                if (!HasName)
                {
                    return "";
                }
                if (nameInt != null)
                {
                    return nameInt;
                }
                return base.Label;
            }
        }

        public override Vector3 DrawPos => tweener.TweenedPos;

        public Faction FactionOwner => base.Faction;

        public bool IsPlayerControlled => base.Faction == Faction.OfPlayer;

        public override bool AppendFactionToInspectString => true;

        public bool CantMove => NightResting;

        public RimWarData rimwarData => WorldUtility.GetRimWarDataForFaction(this.Faction);

        public virtual bool NightResting
        {
            get
            {
                if (!base.Spawned)
                {
                    return false;
                }
                if (pather.Moving && pather.nextTile == pather.Destination && Caravan_PathFollower.IsValidFinalPushDestination(pather.Destination) && Mathf.CeilToInt(pather.nextTileCostLeft / 1f) <= 10000)
                {
                    return false;
                }
                return CaravanNightRestUtility.RestingNowAt(base.Tile);
            }
        }

        public virtual int TicksPerMove //CaravanTicksPerMoveUtility.GetTicksPerMove(this);
        {
            get
            {
                return 10;
            }
            set
            {

            }
        }

        public string TicksPerMoveExplanation
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("explination = warobject type and faction type"); //CaravanTicksPerMoveUtility.GetTicksPerMove(this, stringBuilder);
                return stringBuilder.ToString();
            }
        }

        public virtual float Visibility => 0; //CaravanVisibilityCalculator.Visibility(this);

        public string VisibilityExplanation
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("explination = warobject type and faction type"); //CaravanVisibilityCalculator.Visibility(this, stringBuilder);
                return stringBuilder.ToString();
            }
        }

        public WarObject()
        {
            pather = new WarObject_PathFollower(this);
            gotoMote = new WarObject_GotoMoteRenderer();
            tweener = new WarObject_Tweener(this);
        }

        public void SetUniqueId(int newId)
        {
            if (uniqueId != -1 || newId < 0)
            {
                Log.Error("Tried to set warobject with uniqueId " + uniqueId + " to have uniqueId " + newId);
            }
            uniqueId = newId;
        }

        //NextSearchTick
        //NextSearchTickIncrement (override by type)
        //ScanRange (override by type)
        //EngageNearbyWarObject --> IncidentUtility -- > ImmediateAction
        //EngageNearbyCaravan --> IncidentUtility --> ImmediateAction
        //NotifyPlayer
        //NextMoveTick
        //NextMoveTickIncrement (default is settings based)
        //ArrivalAction

        public override void Tick()
        {
            base.Tick();
            if ((Find.TickManager.TicksGame) >= this.NextSearchTick)
            {
                NextSearchTick = Find.TickManager.TicksGame + NextSearchTickIncrement;
                this.ValidateParentSettlement();
                //scan for nearby engagements
                WorldComponent_PowerTracker.tasker.Register(() =>
                {
                    ScanAction(ScanRange);
                    return null;
                }, (context) =>
                {
                    Notify_Player();
                });
            }
            if ((Find.TickManager.TicksGame + ID) % 251 == 0)
                ValidateTargets();
            if (GenTicks.TicksGame >= this.NextMoveTick)
            {
                NextMoveTick = GenTicks.TicksGame + NextMoveTickIncrement;
                pather.PatherTick();
                tweener.TweenerTick();
                if (this.DestinationReached)
                {
                    ValidateParentSettlement();
                    try
                    {
                        ArrivalAction();
                    }
                    catch (NullReferenceException ex)
                    {
                        Log.Message(this.Name + " threw an error during arrival - rwd(" + this.rimwarData + ") dest(" + this.DestinationTarget + ") parent(" + this.ParentSettlement + ")");
                        this.Destroy();
                    }
                }

                if (!UseDestinationTile)
                {
                    if (this.DestinationTarget != null)
                    {
                        if (DestinationTarget.Tile != pather.Destination)
                        {
                            this.launched = false;
                            PathToTargetTile(DestinationTarget.Tile);
                        }
                    }
                    else
                    {
                        canReachDestination = false;
                        pather.StopDead();
                    }
                }
                if (!canReachDestination)
                {
                    ValidateParentSettlement();
                    if (this.ParentSettlement == null)
                    {
                        FindParentSettlement();
                    }
                    if (ParentSettlement != null && (!Utility.WorldReachability.CanReach(this.Tile, ParentSettlement.Tile) || Find.WorldGrid.ApproxDistanceInTiles(this.Tile, ParentSettlement.Tile) > 100))
                    {
                        this.Destroy();
                    }
                    this.canReachDestination = true;
                    this.DestinationTarget = ParentSettlement;
                    PathToTarget(this.DestinationTarget);
                }
            }
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            tweener.ResetTweenedPosToRoot();
        }

        public void Notify_Teleported()
        {
            tweener.ResetTweenedPosToRoot();
            pather.Notify_Teleported_Int();
        }

        public virtual void Notify_Player()
        {

        }

        public virtual void ScanAction(float range)
        {
            if (interactable)
            {
                List<WorldObject> worldObjects = WorldUtility.GetWorldObjectsInRange(this.Tile, range);
                if (worldObjects != null && worldObjects.Count > 0)
                {
                    for (int i = 0; i < worldObjects.Count; i++)
                    {

                        WorldObject wo = worldObjects[i];
                        if (wo.Faction != this.Faction && wo != this.DestinationTarget)
                        {
                            if (wo is Caravan) //or rimwar caravan, or diplomat, or merchant; ignore scouts and settlements
                            {
                                EngageNearbyCaravan(wo as Caravan);
                                break;
                            }
                            else if (wo is WarObject)
                            {
                                EngageNearbyWarObject(wo as WarObject);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                Log.Message(this.Name + " is not interactable");
            }
        }

        public virtual bool ShouldInteractWith(Caravan car, WarObject rwo)
        {
            List<CaravanTargetData> ctdList = WorldUtility.Get_WCPT().GetCaravaTargetData;
            if (ctdList != null && ctdList.Count > 0)
            {
                for (int i = 0; i < ctdList.Count; i++)
                {
                    if (ctdList[i].caravan == car && ctdList[i].caravanTarget == rwo)
                    {
                        return (car.Faction != null && car.Faction == Faction.OfPlayer);
                    }
                }
            }
            return false;
        }

        public virtual void ValidateTargets()
        {
            if (true) //Find.TickManager.TicksGame % 60 == 0)
            {
                if (this.ParentSettlement == null)
                {
                    FindParentSettlement(); //a null parent will destroy the object
                }
                //target is gone; return home
                if (this.DestinationTarget == null && !UseDestinationTile)
                {
                    pather.StopDead();
                    this.DestinationTarget = this.ParentSettlement;
                    if (this.DestinationTarget == null)
                    {
                        ReAssignParentSettlement();  //updates factions settlement lists; a null parent will destroy the object
                    }
                }
                if (DestinationTarget != null && DestinationTarget.Tile != pather.Destination)
                {
                    pather.StartPath(DestinationTarget.Tile, true, false);
                }
            }
        }

        public virtual void EngageNearbyCaravan(Caravan car)
        {

        }

        public virtual void EngageNearbyWarObject(WarObject rwo)
        {

        }

        public virtual void ImmediateAction(WorldObject wo)
        {
            //Log.Message("immediate action for " + this.Name + "; dest " + DestinationTarget + " parent " + this.ParentSettlement);
            this.ArrivalAction();
        }

        public virtual void ArrivalAction()
        {
            //Log.Message("arrival action for " + this.Name + "; dest " + DestinationTarget + " parent " + this.ParentSettlement);
            if (!this.Destroyed)
            {
                this.Destroy();
            }
            if (Find.WorldObjects.Contains(this))
            {
                Find.WorldObjects.Remove(this);
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            //stringBuilder.Append(base.GetInspectString());            

            WorldObject wo = Find.World.worldObjects.ObjectsAt(pather.Destination).FirstOrDefault();
            if (wo != null)
            {
                if (wo.Faction != this.Faction)
                {
                    if (this is Trader)
                    {
                        stringBuilder.Append("RW_WarObjectInspectString".Translate(this.Name, "RW_Trading".Translate(), wo.Label));
                    }
                    else if (this is Scout)
                    {
                        stringBuilder.Append("RW_WarObjectInspectString".Translate(this.Name, "RW_Scouting".Translate(), wo.Label));
                    }
                    else
                    {
                        stringBuilder.Append("RW_WarObjectInspectString".Translate(this.Name, "RW_Attacking".Translate(), wo.Label));
                    }
                }
                else
                {
                    stringBuilder.Append("RW_WarObjectInspectString".Translate(this.Name, "RW_ReturningTo".Translate(), wo.Label));
                }
            }


            if (pather.Moving)
            {
                float num6 = (float)Utility.ArrivalTimeEstimator.EstimatedTicksToArrive(base.Tile, pather.Destination, this.TicksPerMove) / 60000f;
                if (stringBuilder.Length != 0)
                {
                    stringBuilder.AppendLine();
                }
                stringBuilder.Append("RW_EstimatedTimeToDestination".Translate(num6.ToString("0.#")));
                stringBuilder.Append(" (" + Find.WorldGrid.TraversalDistanceBetween(this.Tile, pather.Destination) + "RW_TilesAway_Verbatum".Translate() + ")");
                if (this.NightResting)
                {
                    stringBuilder.Append("\n" + "RW_UnitCamped".Translate());
                }
            }
            if (stringBuilder.Length != 0)
            {
                stringBuilder.AppendLine();
            }
            stringBuilder.Append("RW_CombatPower".Translate(this.RimWarPoints));
            stringBuilder.Append("\n" + this.Faction.PlayerRelationKind.ToString());
            if (!pather.MovingNow)
            {

            }
            return stringBuilder.ToString();
        }

        public virtual bool DestinationReached
        {
            get
            {
                return this.Tile == pather.Destination;
            }
        }

        public void PathToTarget(WorldObject wo)
        {
            pather.StartPath(wo.Tile, true);
            tweener.ResetTweenedPosToRoot();
        }

        public void PathToTargetTile(int tile)
        {
            pather.StartPath(tile, true);
            tweener.ResetTweenedPosToRoot();
        }

        public void ValidateParentSettlement()
        {
            if (this.ParentSettlement != null)
            {
                RimWorld.Planet.Settlement settlement = Find.WorldObjects.SettlementAt(this.ParentSettlement.Tile);
                if (settlement == null || settlement.Faction != this.Faction || settlement.Destroyed)
                {
                    ////RimWar.Planet.Settlement rws = null;
                    ////if (WorldUtility.GetRimWarDataForFaction(this.Faction).HasFactionSettlementFor(settlement, out rws))
                    ////{
                    ////    WorldUtility.GetRimWarDataForFaction(this.Faction).FactionSettlements.Remove(rws);
                    ////}
                    this.ParentSettlement = null;
                }
            }
        }

        public void FindParentSettlement()
        {
            RimWorld.Planet.Settlement wos = null;
            wos = WorldUtility.GetClosestSettlementInRWDTo(WorldUtility.GetRimWarDataForFaction(this.Faction), this.Tile);
            if (wos != null)
            {
                this.parentSettlementTile = wos.Tile;
                ParentSettlement = wos;
            }

            if (this.parentSettlement == null)
            {
                //warband is lost, no nearby parent settlement
                this.Destroy();
                if (Find.WorldObjects.Contains(this))
                {
                    Find.WorldObjects.Remove(this);
                }
            }
        }

        public void ReAssignParentSettlement()
        {
            this.ValidateParentSettlement();
            WorldUtility.Get_WCPT().UpdateFactionSettlements(WorldUtility.GetRimWarDataForFaction(this.Faction));
            FindParentSettlement();
            this.DestinationTarget = this.ParentSettlement;
        }

        public void FindHostileSettlement()
        {
            this.DestinationTarget = Find.World.worldObjects.WorldObjectOfDefAt(WorldObjectDefOf.Settlement, WorldUtility.GetHostileSettlementsInRange(this.Tile, 25, this.Faction, WorldUtility.GetRimWarData(), WorldUtility.GetRimWarDataForFaction(this.Faction)).RandomElement().Tile);
            if (this.DestinationTarget != null)
            {
                PathToTarget(this.DestinationTarget);
            }
            else
            {
                if (this.ParentSettlement == null)
                {
                    FindParentSettlement();
                }
                else
                {
                    PathToTarget(Find.World.worldObjects.WorldObjectAt(this.ParentSettlement.Tile, WorldObjectDefOf.Settlement));
                }
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            //using (IEnumerator<FloatMenuOption> enumerator = base.GetFloatMenuOptions(caravan).GetEnumerator())
            //{
            //    if (enumerator.MoveNext())
            //    {
            //        FloatMenuOption o = enumerator.Current;
            //        yield return o;
            //    }
            //}
            using (IEnumerator<FloatMenuOption> enumerator2 = CaravanArrivalAction_AttackWarObject.GetFloatMenuOptions(caravan, this).GetEnumerator())
            {
                if (enumerator2.MoveNext())
                {
                    FloatMenuOption f2 = enumerator2.Current;
                    yield return f2;
                }
            }
            using (IEnumerator<FloatMenuOption> enumerator3 = CaravanArrivalAction_EngageWarObject.GetFloatMenuOptions(caravan, this).GetEnumerator())
            {
                if (enumerator3.MoveNext())
                {
                    FloatMenuOption f3 = enumerator3.Current;
                    yield return f3;
                }
            }
            yield break;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (Prefs.DevMode)
            {
                List<Gizmo> gizmoIE = base.GetGizmos().ToList();
                Command_Action command_Action1 = new Command_Action();
                command_Action1.defaultLabel = "Dev: Destroy";
                command_Action1.defaultDesc = "Destroys the Rim War object.";
                command_Action1.action = delegate
                {
                    Destroy();
                };
                gizmoIE.Add(command_Action1);
                return gizmoIE;
            }
            return base.GetGizmos();
        }
    }
}
