using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using RimWar.Planet;

namespace RimWar.Utility
{
    public static class ArrivalTimeEstimator
    {
        public static int EstimatedTicksToArrive(PlanetTile from, PlanetTile to, WarObject warObject)
        { 
            using (WorldPath worldPath = warObject.pather.curPath ?? GeneratePathForWarObject(from, to, warObject))// Verse.Find.WorldPathFinder.FindPath(from, to, null))
            {
                if(!worldPath.Found)
                {
                    return 0;
                }
                //return CaravanArrivalTimeEstimator.EstimatedTicksToArrive(from, to, worldPath, 0, warObject.TicksPerMove, Verse.Find.TickManager.TicksAbs);
                float distance = Find.WorldGrid.ApproxDistanceInTiles(from, to);
                Log.Message("distance is " + distance);
                float travelTimePerTile = warObject.MovementModifier;

                return Mathf.RoundToInt(distance * travelTimePerTile);
            }
        }

        // Add this method for LaunchedWarObjects (including LaunchedWarband)
        public static int EstimatedTicksToArrive(PlanetTile from, PlanetTile to, LaunchedWarObject launchedWarObject)
        {
            // LaunchedWarObjects fly directly, so simple distance calculation
            float distance = Find.WorldGrid.ApproxDistanceInTiles(from, to);

            // Use the LaunchedWarObject's travel speed (from LaunchedWarObject.TravelSpeed)
            // LaunchedWarObjects travel at 0.00025f tiles per tick
            float travelTimePerTile = 1f / 0.00025f; // 4000 ticks per tile

            return Mathf.RoundToInt(distance * travelTimePerTile);
        }

        private static WorldPath GeneratePathForWarObject(PlanetTile fromTile, PlanetTile toTile, WarObject warObject)
        {
            // This mirrors what the WarObject does internally for pathfinding
            PlanetLayer layer = PlanetLayer.Selected ?? Find.WorldGrid.Surface;
            PlanetTile startTile = new PlanetTile(fromTile, layer);
            PlanetTile endTile = new PlanetTile(toTile, layer);

            using (var pathing = new WorldPathing(layer))
            {
                return pathing.FindPath(startTile, endTile, null);
            }
        }
    }
}
