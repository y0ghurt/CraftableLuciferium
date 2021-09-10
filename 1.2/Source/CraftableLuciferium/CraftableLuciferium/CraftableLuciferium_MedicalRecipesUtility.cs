﻿using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CraftableLuciferium
{
    class CraftableLuciferium_MedicalRecipesUtility
    {
        public static bool IsCleanAndDroppable(Pawn pawn, BodyPartRecord part)
        {
            return !pawn.Dead && !pawn.RaceProps.Animal && part.def.spawnThingOnRemoved != null && CraftableLuciferium_MedicalRecipesUtility.IsClean(pawn, part);
        }

        public static bool IsClean(Pawn pawn, BodyPartRecord part)
        {
            return !pawn.Dead && !(from x in pawn.health.hediffSet.hediffs
                                   where x.Part == part
                                   select x).Any<Hediff>();
        }

        public static void RestorePartAndSpawnAllPreviousParts(Pawn pawn, BodyPartRecord part, IntVec3 pos, Map map)
        {
            CraftableLuciferium_MedicalRecipesUtility.SpawnNaturalPartIfClean(pawn, part, pos, map);
            CraftableLuciferium_MedicalRecipesUtility.SpawnThingsFromHediffs(pawn, part, pos, map);
            pawn.health.RestorePart(part, null, true);
        }

        public static Thing SpawnNaturalPartIfClean(Pawn pawn, BodyPartRecord part, IntVec3 pos, Map map)
        {
            if (CraftableLuciferium_MedicalRecipesUtility.IsCleanAndDroppable(pawn, part))
            {
                return GenSpawn.Spawn(part.def.spawnThingOnRemoved, pos, map);
            }
            return null;
        }

        public static void SpawnThingsFromHediffs(Pawn pawn, BodyPartRecord part, IntVec3 pos, Map map)
        {
            if (!pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined).Contains(part))
            {
                return;
            }
            IEnumerable<Hediff> enumerable = from x in pawn.health.hediffSet.hediffs
                                             where x.Part == part
                                             select x;
            foreach (Hediff current in enumerable)
            {
                if (current.def.spawnThingOnRemoved != null)
                {
                    GenSpawn.Spawn(current.def.spawnThingOnRemoved, pos, map);
                }
            }
            for (int i = 0; i < part.parts.Count; i++)
            {
                CraftableLuciferium_MedicalRecipesUtility.SpawnThingsFromHediffs(pawn, part.parts[i], pos, map);
            }
        }
    }
}
