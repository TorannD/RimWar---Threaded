﻿using RimWorld;
using RimWar.Planet;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWar
{
    public static class RimWarFactionUtility
    {
        private static bool showAll;

        private const float FactionColorRectSize = 15f;

        private const float FactionColorRectGap = 10f;

        private const float RowMinHeight = 80f;

        private const float LabelRowHeight = 50f;

        private const float TypeColumnWidth = 100f;

        private const float NameColumnWidth = 250f;

        private const float RelationsColumnWidth = 90f;

        private const float NameLeftMargin = 15f;        

        public static void DoWindowContents(Rect fillRect, ref Vector2 scrollPosition, ref float scrollViewHeight)
        {
            Rect position = new Rect(0f, 0f, fillRect.width, fillRect.height);
            GUI.BeginGroup(position);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect outRect = new Rect(0f, 50f, position.width, position.height - 50f);
            Rect rect = new Rect(0f, 0f, position.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, rect);
            float num = 0f;
            foreach (Faction item in WorldUtility.GetRimWarFactions(false))// Find.FactionManager.AllFactionsInViewOrder)
            {
                if ((!item.IsPlayer && !item.def.hidden))
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.2f);
                    Widgets.DrawLineHorizontal(0f, num, rect.width);
                    GUI.color = Color.white;
                    num += DrawFactionRow(item, num, rect);
                }
            }
            if (Event.current.type == EventType.Layout)
            {
                scrollViewHeight = num;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
        }

        private static float DrawFactionRow(Faction faction, float rowY, Rect fillRect)
        {
            Rect rect = new Rect(35f, rowY, 300f, 160f);
            StringBuilder stringBuilder = new StringBuilder();
            if (faction != null && !faction.defeated)
            {
                RimWarData rwd = WorldUtility.GetRimWarDataForFaction(faction);                
                if (rwd != null)
                {
                    int costToAlly = rwd.TotalFactionPoints / 10;
                    int playerSilver = GetPlayerSilver;
                    bool canDeclareWar = !rwd.IsAtWarWith(Faction.OfPlayer);
                    bool canDeclareAlliance = faction.PlayerRelationKind == FactionRelationKind.Ally && !rwd.AllianceFactions.Contains(Faction.OfPlayer) && playerSilver > costToAlly;
                    foreach (Faction item in Find.FactionManager.AllFactionsInViewOrder)
                    {
                        if (item != null)
                        {
                            if (item != faction && ((!item.IsPlayer && !item.def.hidden)) && faction.HostileTo(item))
                            {
                                stringBuilder.Append("HostileTo".Translate(item.Name));
                                stringBuilder.AppendLine();
                            }
                            else if (item != faction && ((!item.IsPlayer && !item.def.hidden)) && faction.RelationKindWith(item) == FactionRelationKind.Ally)
                            {
                                stringBuilder.Append("RW_AllyTo".Translate(item.Name));
                                stringBuilder.AppendLine();
                            }
                            else if (item != faction && ((!item.IsPlayer && !item.def.hidden)) && faction.RelationKindWith(item) == FactionRelationKind.Neutral)
                            {
                                stringBuilder.Append("RW_NeutralTo".Translate(item.Name));
                                stringBuilder.AppendLine();
                            }
                        }
                    }
                    string text = stringBuilder.ToString();
                    float width = fillRect.width - rect.xMax;
                    float num = Text.CalcHeight(text, width);
                    float num2 = Mathf.Max(176f, num);
                    Rect position = new Rect(10f, rowY + 10f, 15f, 15f);
                    Rect positionRival = new Rect(10f, rowY + 30f, 15f, 15f);
                    Rect rect2 = new Rect(0f, rowY, fillRect.width, num2);
                    if (Mouse.IsOver(rect2))
                    {
                        GUI.DrawTexture(rect2, TexUI.HighlightTex);
                    }
                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.UpperLeft;
                    //Widgets.DrawRectFast(position, faction.Color);
                    FactionUIUtility.DrawFactionIconWithTooltip(position, faction);
                    if (WorldUtility.Get_WCPT().victoryFaction == faction)
                    {
                        DrawRivalIconWithTooltip(positionRival, faction);
                    }
                    string label = faction.Name.CapitalizeFirst() + "\n" + faction.def.LabelCap + "\n" + ((faction.leader == null) ? string.Empty : (faction.def.leaderTitle.CapitalizeFirst() + ": "
                        + faction.leader.Name.ToStringFull))
                        + "\n" + "RW_FactionBehavior".Translate(rwd == null ? RimWarBehavior.Undefined.ToString() : rwd.behavior.ToString())
                        + "\n" + "RW_FactionPower".Translate(rwd == null ? 0 : rwd.TotalFactionPoints)
                        + "\n" + "RW_SettlementCount".Translate((rwd != null && rwd.WorldSettlements != null && rwd.WorldSettlements.Count > 0) ? rwd.WorldSettlements.Count : 0)
                        + "\n" + "RW_WarObjectCount".Translate((rwd != null && WorldUtility.GetWarObjectsInFaction(faction) != null) ? WorldUtility.GetWarObjectsInFaction(faction).Count : 0);
                    //+ ((faction != WorldUtility.Get_WCPT().victoryFaction) ? string.Empty : "\n" + (string)"RW_RivalFaction".Translate());
                    if (Options.Settings.Instance.randomizeAttributes)
                    {
                        label += "\n" + "RW_AttributeDisplay".Translate(rwd.combatAttribute.ToString("P0"), rwd.movementAttribute.ToString("P0"), rwd.growthAttribute.ToString("P0"));
                    }

                    Widgets.Label(rect, label);
                    Rect rect3 = new Rect(rect.xMax, rowY, 40f, 80f);  //Rect rect3 = new Rect(rect.xMax, rowY, 60f, 80f);
                    Widgets.InfoCardButton(rect3.x, rect3.y, faction.def);
                    Rect rect4 = new Rect(rect3.xMax, rowY, 120f, 80f); //Rect rect4 = new Rect(rect3.xMax, rowY, 250f, 80f);
                    if (!faction.IsPlayer)
                    {
                        string str = faction.HasGoodwill ? (faction.PlayerGoodwill.ToStringWithSign() + "\n") : "";
                        str += faction.PlayerRelationKind.GetLabel();
                        if (faction.defeated)
                        {
                            str = str + "\n(" + "DefeatedLower".Translate() + ")";
                        }
                        GUI.color = faction.PlayerRelationKind.GetColor();
                        Widgets.Label(rect4, str);
                        GUI.color = Color.white;
                        string str2 = "CurrentGoodwillTip".Translate();
                        if (faction.def.permanentEnemy)
                        {
                            str2 = str2 + "\n\n" + "CurrentGoodwillTip_PermanentEnemy".Translate();
                        }
                        else
                        {
                            str2 += "\n\n";
                            switch (faction.PlayerRelationKind)
                            {
                                case FactionRelationKind.Ally:
                                    str2 += "CurrentGoodwillTip_Ally".Translate(0.ToString("F0"));
                                    break;
                                case FactionRelationKind.Neutral:
                                    str2 += "CurrentGoodwillTip_Neutral".Translate((-75).ToString("F0"), 75.ToString("F0"));
                                    break;
                                case FactionRelationKind.Hostile:
                                    str2 += "CurrentGoodwillTip_Hostile".Translate(0.ToString("F0"));
                                    break;
                            }
                            //1.3 //
                            //if (faction.def.goodwillDailyGain > 0f || faction.def.goodwillDailyFall > 0f)
                            //{
                            //    float num3 = faction.def.goodwillDailyGain * 60f;
                            //    float num4 = faction.def.goodwillDailyFall * 60f;
                            //    str2 += "\n\n" + "CurrentGoodwillTip_NaturalGoodwill".Translate(faction.def.naturalColonyGoodwill.min.ToString("F0"), faction.def.naturalColonyGoodwill.max.ToString("F0"));
                            //    if (faction.def.naturalColonyGoodwill.min > -100)
                            //    {
                            //        str2 += " " + "CurrentGoodwillTip_NaturalGoodwillRise".Translate(faction.def.naturalColonyGoodwill.min.ToString("F0"), num3.ToString("F0"));
                            //    }
                            //    if (faction.def.naturalColonyGoodwill.max < 100)
                            //    {
                            //        str2 += " " + "CurrentGoodwillTip_NaturalGoodwillFall".Translate(faction.def.naturalColonyGoodwill.max.ToString("F0"), num4.ToString("F0"));
                            //    }
                            //}
                        }
                        TooltipHandler.TipRegion(rect4, str2);
                    }
                    Rect rect6 = new Rect(rect4.xMax, rowY + 10, 100f, 28f);
                    if (canDeclareWar)
                    {
                        bool declareWar = Widgets.ButtonText(rect6, "War", canDeclareWar, false, canDeclareWar);
                        if (declareWar)
                        {
                            DeclareWarOn(Faction.OfPlayer, faction);
                        }
                        TooltipHandler.TipRegion(rect6, "RW_DeclareWarWarning".Translate());
                    }
                    else
                    {
                        bool declarePeace = Widgets.ButtonText(rect6, "Peace", faction.GoodwillWith(Faction.OfPlayer) >= -75, false, true);
                        if (declarePeace && faction.GoodwillWith(Faction.OfPlayer) >= -75)
                        {
                            DeclarePeaceWith(Faction.OfPlayer, faction);
                        }
                        if (faction.GoodwillWith(Faction.OfPlayer) < -75)
                        {
                            TooltipHandler.TipRegion(rect6, "RW_DeclarePeaceInfo".Translate(faction.GoodwillWith(Faction.OfPlayer)));
                        }
                        else
                        {
                            TooltipHandler.TipRegion(rect6, "RW_DeclarePeaceWarning".Translate());
                        }
                    }

                    Rect rect7 = new Rect(rect4.xMax, rowY + 10 + rect6.height, 100f, 28f);
                    if (!rwd.IsAtWarWith(Faction.OfPlayer))
                    {
                        bool declareAlly = Widgets.ButtonText(rect7, "Alliance", canDeclareAlliance, false, true);
                        if (canDeclareAlliance)
                        {
                            if (declareAlly && canDeclareAlliance)
                            {
                                TributeSilver(costToAlly);
                                DeclareAllianceWith(Faction.OfPlayer, faction);
                            }
                            TooltipHandler.TipRegion(rect7, "RW_DeclareAllianceWarning".Translate(costToAlly));
                        }
                        else
                        {
                            StringBuilder strAlly = new StringBuilder();
                            if (!rwd.IsAlliedWith(Faction.OfPlayer))
                            {
                                if (faction.PlayerRelationKind != FactionRelationKind.Ally)
                                {
                                    strAlly.Append("RW_Reason_NotAlly".Translate() + "\n");
                                }
                                if (costToAlly > playerSilver)
                                {
                                    strAlly.Append("RW_Reason_NotEnoughTribute".Translate(playerSilver, costToAlly) + "\n");
                                }
                                List<RimWarData> rwdList = WorldUtility.GetRimWarData();
                                string alliedFactions = "";
                                for (int i = 0; i < rwdList.Count; i++)
                                {
                                    if (rwdList[i].AllianceFactions.Contains(Faction.OfPlayer) && faction.HostileTo(rwdList[i].RimWarFaction))
                                    {
                                        alliedFactions += rwdList[i].RimWarFaction.Name + "\n";
                                    }
                                }
                                strAlly.Append(alliedFactions);
                            }
                            else
                            {
                                strAlly.Append("RW_Reason_AlreadyAllied".Translate());
                            }
                            TooltipHandler.TipRegion(rect7, "RW_DeclareAllianceInfo".Translate(strAlly));
                        }
                    }
                    Rect rect5 = new Rect(rect6.xMax + 20, rowY, width, num);
                    Widgets.Label(rect5, text);
                    Text.Anchor = TextAnchor.UpperLeft;

                    return num2;
                }
            }
            return 0f;
        }

        public static void DeclareWarOn(Faction declaringFaction, Faction withFaction)
        {
            List<RimWarData> rwdList = WorldUtility.GetRimWarData();
            
            for (int i = 0; i < rwdList.Count; i++)
            {
                RimWarData rwd = rwdList[i];
                if(rwd.RimWarFaction == declaringFaction)
                {
                    if (!rwd.IsAtWarWith(withFaction))
                    {
                        rwd.WarFactions.Add(withFaction);
                        declaringFaction.RelationWith(withFaction).baseGoodwill = -100;
                        declaringFaction.RelationWith(withFaction).kind = FactionRelationKind.Hostile;
                        Find.LetterStack.ReceiveLetter("RW_DiplomacyLetter".Translate("RW_DiplomacyLabel_War".Translate()), "RW_DeclareWar".Translate(rwd.RimWarFaction.Name, withFaction.Name), RimWarDefOf.RimWar_HostileEvent);
                    }

                    for(int j = 0; j < rwd.AllianceFactions.Count; j++)
                    {
                        if (!WorldUtility.GetRimWarDataForFaction(rwd.AllianceFactions[j]).IsAtWarWith(withFaction))
                        {
                            DeclareWarOn(rwd.AllianceFactions[j], withFaction);
                         }
                    }
                }
                if(rwd.RimWarFaction == withFaction)
                {
                    if (!rwd.IsAtWarWith(declaringFaction))
                    {
                        withFaction.RelationWith(declaringFaction).baseGoodwill = -100;
                        withFaction.RelationWith(declaringFaction).kind = FactionRelationKind.Hostile;
                        rwd.WarFactions.Add(declaringFaction);
                        Find.LetterStack.ReceiveLetter("RW_DiplomacyLetter".Translate("RW_DiplomacyLabel_War".Translate()), "RW_DeclareWar".Translate(rwd.RimWarFaction.Name, declaringFaction.Name), RimWarDefOf.RimWar_HostileEvent);
                    }

                    for (int j = 0; j < rwd.AllianceFactions.Count; j++)
                    {
                        if (!WorldUtility.GetRimWarDataForFaction(rwd.AllianceFactions[j]).IsAtWarWith(declaringFaction))
                        {
                            DeclareWarOn(rwd.AllianceFactions[j], declaringFaction);
                        }
                    }
                }
            }
        }

        public static void DeclareAllianceWith(Faction declaringFaction, Faction withFaction)
        {
            RimWarData rwd = WorldUtility.GetRimWarDataForFaction(declaringFaction);
            if (!rwd.IsAlliedWith(withFaction))
            {
                rwd.AllianceFactions.Add(withFaction);
                declaringFaction.RelationWith(withFaction).baseGoodwill = 100;
                declaringFaction.RelationWith(withFaction).kind = FactionRelationKind.Ally;
                Find.LetterStack.ReceiveLetter("RW_DiplomacyLetter".Translate("RW_DiplomacyLabel_Alliance".Translate()), "RW_DeclareAlliance".Translate(rwd.RimWarFaction.Name, withFaction.Name), RimWarDefOf.RimWar_NeutralEvent);
            }
            RimWarData rwdAlly = WorldUtility.GetRimWarDataForFaction(withFaction);
            if (!rwdAlly.IsAlliedWith(declaringFaction))
            {
                withFaction.RelationWith(declaringFaction).baseGoodwill = 100;
                withFaction.RelationWith(declaringFaction).kind = FactionRelationKind.Ally;
                rwdAlly.AllianceFactions.Add(declaringFaction);
                Find.LetterStack.ReceiveLetter("RW_DiplomacyLetter".Translate("RW_DiplomacyLabel_Alliance".Translate()), "RW_DeclareAlliance".Translate(rwdAlly.RimWarFaction.Name, declaringFaction.Name), RimWarDefOf.RimWar_NeutralEvent);
            }

            for(int i = 0; i < rwd.WarFactions.Count; i++)
            {
                if(!rwdAlly.IsAtWarWith(rwd.WarFactions[i]))
                {
                    DeclareWarOn(rwdAlly.RimWarFaction, rwd.WarFactions[i]);
                    //Find.LetterStack.ReceiveLetter("RW_DiplomacyLetter".Translate("RW_DiplomacyLabel_War".Translate()), "RW_DeclareWar".Translate(rwdAlly.RimWarFaction.Name, rwd.WarFactions[i].Name), RimWarDefOf.RimWar_HostileEvent);
                }
            }
        }

        public static void DeclarePeaceWith(Faction declaringFaction, Faction withFaction)
        {
            List<RimWarData> rwdList = WorldUtility.GetRimWarData();

            for (int i = 0; i < rwdList.Count; i++)
            {
                RimWarData rwd = rwdList[i];
                if (rwd.RimWarFaction == declaringFaction)
                {
                    if (rwd.IsAtWarWith(withFaction))
                    {
                        rwd.WarFactions.Remove(withFaction);
                        Find.LetterStack.ReceiveLetter("RW_DiplomacyLetter".Translate("RW_DiplomacyLabel_Peace".Translate()), "RW_DeclarePeace".Translate(rwd.RimWarFaction.Name, withFaction.Name), RimWarDefOf.RimWar_TradeEvent);
                    }
                }
                if (rwd.RimWarFaction == withFaction)
                {
                    if (rwd.IsAtWarWith(declaringFaction))
                    {
                        rwd.WarFactions.Remove(declaringFaction);
                        Find.LetterStack.ReceiveLetter("RW_DiplomacyLetter".Translate("RW_DiplomacyLabel_Peace".Translate()), "RW_DeclarePeace".Translate(rwd.RimWarFaction.Name, declaringFaction.Name), RimWarDefOf.RimWar_TradeEvent);
                    }
                }
            }
        }

        public static void EndAllianceWith(Faction declaringFaction, Faction withFaction)
        {
            List<RimWarData> rwdList = WorldUtility.GetRimWarData();

            for (int i = 0; i < rwdList.Count; i++)
            {
                RimWarData rwd = rwdList[i];
                if (rwd.RimWarFaction == declaringFaction)
                {
                    if (rwd.IsAlliedWith(withFaction))
                    {
                        rwd.AllianceFactions.Remove(withFaction);
                        Find.LetterStack.ReceiveLetter("RW_DiplomacyLetter".Translate("RW_DiplomacyLabel_EndAlliance".Translate()), "RW_DeclareAllianceEnd".Translate(rwd.RimWarFaction.Name, withFaction.Name), RimWarDefOf.RimWar_NeutralEvent);
                    }
                }
                if (rwd.RimWarFaction == withFaction)
                {
                    if (rwd.IsAlliedWith(declaringFaction))
                    {
                        rwd.AllianceFactions.Remove(declaringFaction);
                        Find.LetterStack.ReceiveLetter("RW_DiplomacyLetter".Translate("RW_DiplomacyLabel_EndAlliance".Translate()), "RW_DeclareAllianceEnd".Translate(rwd.RimWarFaction.Name, declaringFaction.Name), RimWarDefOf.RimWar_NeutralEvent);
                    }
                }
            }
        }

        public static void RandomizeAllFactionRelations()
        {
            Log.Message("Rim War: randomizing faction relations.");
            List<Faction> factions = Find.FactionManager.AllFactionsVisible.ToList();
            for(int i = 0; i < factions.Count; i++)
            {
                Faction firstFaction = factions[i];
                for(int j = 0; j< factions.Count; j++)
                {
                    Faction otherFaction = factions[j];
                    if(firstFaction != otherFaction)
                    {
                        if(!(firstFaction == Faction.OfPlayer && !otherFaction.def.permanentEnemy) && !(firstFaction != Faction.OfPlayer && otherFaction.def.permanentEnemyToEveryoneExceptPlayer) && 
                            !(firstFaction.def.permanentEnemy && otherFaction == Faction.OfPlayer) && !(firstFaction.def.permanentEnemyToEveryoneExceptPlayer && otherFaction != Faction.OfPlayer))
                        {
                            firstFaction.TryAffectGoodwillWith(otherFaction, -1 * firstFaction.GoodwillWith(otherFaction), true, true, RimWarDefOf.RW_RandomizeRelations);
                            firstFaction.TryAffectGoodwillWith(otherFaction, Rand.Range(-100, 100), true, true, RimWarDefOf.RW_RandomizeRelations);
                            //firstFaction.TryAffectGoodwillWith(otherFaction, -1 * firstFaction.GoodwillWith(otherFaction), false, false, "Rim War - Clear Relation");
                            //firstFaction.TryAffectGoodwillWith(otherFaction, Rand.Range(-100, 100), false, false, "Rim War - Randomize Relation");
                            //Log.Message("" + firstFaction.Name + " has " + firstFaction.RelationKindWith(otherFaction).ToString() + " relations with " + otherFaction.Name);
                            //Log.Message("" + otherFaction.Name + " has " + otherFaction.RelationKindWith(firstFaction).ToString() + " relations with " + firstFaction.Name);
                        }
                    }
                }
            }
        }

        public static void DrawRivalIconWithTooltip(Rect r, Faction faction)
        {
            GUI.DrawTexture(r, RimWarMatPool.Material_Exclamation_Red);
            GUI.color = Color.white;
            if (Mouse.IsOver(r))
            {
                TipSignal tip = new TipSignal(() => faction.Name + "RW_FactionIsRival".Translate(), faction.loadID ^ 0x738AC054);
                TooltipHandler.TipRegion(r, tip);
                Widgets.DrawHighlight(r);
            }
        }

        public static void TributeSilver(int amount)
        {
            List<Thing> allZones = Find.AnyPlayerHomeMap.listerThings.AllThings;
            if (allZones != null && allZones.Count > 0)
            {
                for (int i = 0; i < allZones.Count; i++)
                {
                    //foreach (Thing t in allZones[i].AllContainedThings)
                    //{
                    Thing t = allZones[i];
                        if (t.def == ThingDefOf.Silver && amount > 0)
                        {
                            int splitAmount = amount > t.stackCount ? t.stackCount : amount;
                            t.SplitOff(splitAmount).Destroy(DestroyMode.Vanish);
                            amount -= splitAmount;
                            doOnce = false;
                        }
                    //}
                }
            }
            //List<Zone> allZones = Find.AnyPlayerHomeMap.zoneManager.AllZones;
            //if (allZones != null && allZones.Count > 0)
            //{
            //    for (int i = 0; i < allZones.Count; i++)
            //    {
            //        foreach (Thing t in allZones[i].AllContainedThings)
            //        {
            //            if (t.def == ThingDefOf.Silver && amount > 0)
            //            {
            //                int splitAmount = amount > t.stackCount ? t.stackCount : amount;                            
            //                t.SplitOff(splitAmount).Destroy(DestroyMode.Vanish);
            //                amount -= splitAmount;
            //                doOnce = false;
            //            }
            //        }
            //    }
            //}
        }

        private static int hashSilver = 0;
        private static bool doOnce = true;
        public static int GetPlayerSilver
        {
            get
            {
                if (Find.TickManager.TicksGame % 60 == 0 || doOnce)
                {
                    int totalSilver = 0;
                    List<Thing> allZones = Find.AnyPlayerHomeMap.listerThings.AllThings;
                    if (allZones != null && allZones.Count > 0)
                    {
                        for (int i = 0; i < allZones.Count; i++)
                        {
                            //foreach (Thing t in allZones[i].AllContainedThings)
                            //{
                            Thing t = allZones[i];
                                if (t.def == ThingDefOf.Silver)
                                {
                                    totalSilver += t.stackCount;
                                }
                            //}
                        }
                    }
                    hashSilver = totalSilver;
                }
                return hashSilver;
            }
        }
    }
}
