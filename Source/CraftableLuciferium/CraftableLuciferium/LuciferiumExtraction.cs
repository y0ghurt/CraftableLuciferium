using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace CraftableLuciferium
{
    public class HediffExtension : DefModExtension
    {
        public List<HediffDef> extraHediffs = new List<HediffDef>();
    }

    public class LuciferiumExtraction : Recipe_Surgery
    {
        private const float ViolationGoodwillImpact = 0f;

        public BodyPartRecord GetPart(Pawn pawn)
        {
            BodyPartDef partDef;
            Random random = new Random();
            List<BodyPartDef> tried = new List<BodyPartDef>();
            for (int retries = 0; retries < 16; retries++)
            {
                bool firstRun = true;
                partDef = new BodyPartDef();
                while (firstRun || tried.Contains(partDef)) {
                    firstRun = false;
                    int partIndex = random.Next(101);
                    //Log.Message("Trying partIndex = " + partIndex);
                    if (partIndex <= 18)
                    {
                        partDef = BodyPartDefOf.Leg;
                    }
                    else if (partIndex <= 50)
                    {
                        partDef = BodyPartDefOf.Hand;
                    }
                    else if (partIndex <= 66)
                    {
                        partDef = BodyPartDefOf.Arm;
                    }
                    else if (partIndex <= 74)
                    {
                        partDef = BodyPartDefOf.Eye;
                    }
                    else
                    {
                        partDef = BodyPartDefOf.Jaw;
                    }
                }
                tried.Add(partDef);

                IEnumerable<BodyPartRecord> bodyPartRecordList = pawn.health.hediffSet.GetNotMissingParts();
                List<BodyPartRecord> selectedBodyParts = new List<BodyPartRecord>();
                foreach (BodyPartRecord record in bodyPartRecordList)
                {
                    if (record.def.Equals(partDef))
                    {
                        selectedBodyParts.Add(record);
                        //   return record;
                    }
                }
                int bodyPartsCount = selectedBodyParts.Count;
                //Log.Message("Found " + bodyPartsCount + " body parts");
                if (bodyPartsCount > 0)
                {
                    Random r = new Random();
                    BodyPartRecord bodyPartRecord = selectedBodyParts[r.Next(0, bodyPartsCount)];
                    //Log.Message("Returning body part " + bodyPartRecord.Label);
                    return bodyPartRecord;
                }
            }
            //Log.Message("Failed to find a good part, returning Brain");
            return pawn.health.hediffSet.GetBrain();
            //return pawn.health.hediffSet.GetRandomNotMissingPart(null, BodyPartHeight.Undefined, BodyPartDepth.Outside);
        }

        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            
            List<Hediff> allHediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < allHediffs.Count; i++)
            {
                if (allHediffs[i].Part == null)
                {
                    if (allHediffs[i].def == recipe.removesHediff)
                    {
                        if (allHediffs[i].Visible)
                        {
                            // Some magic to allow me to hide the operation in the menu properly.
                            yield return pawn.health.hediffSet.GetBrain();
                            break;
                        }
                    }
                }
            }
        }

        public override bool IsViolationOnPawn(Pawn pawn, BodyPartRecord part, Faction billDoerFaction)
        {
            //return pawn.Faction != billDoerFaction && HealthUtility.PartRemovalIntent(pawn, part) == BodyPartRemovalIntent.Amputate;
            return false;
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {

            if (billDoer != null)
            {
                if (base.CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, new object[]
                {
                    billDoer,
                    pawn
                });
                if (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(billDoer))
                {
                    string text;
                    if (!this.recipe.successfullyRemovedHediffMessage.NullOrEmpty())
                    {
                        text = string.Format(this.recipe.successfullyRemovedHediffMessage, billDoer.LabelShort, pawn.LabelShort);
                    }
                    else
                    {
                        text = "MessageSuccessfullyRemovedHediff".Translate(billDoer.LabelShort, pawn.LabelShort, this.recipe.removesHediff.label.Named("HEDIFF"), billDoer.Named("SURGEON"), pawn.Named("PATIENT"));
                    }
                    Messages.Message(text, pawn, MessageTypeDefOf.PositiveEvent, true);
                }
            }

            Hediff hediff = pawn.health.hediffSet.hediffs.Find((Hediff x) => x.def == this.recipe.removesHediff && x.Part == null  /* part */ && x.Visible);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }

            if (bill.recipe.HasModExtension<HediffExtension>())
            {
                foreach (HediffDef extraHediffDef in bill.recipe.GetModExtension<HediffExtension>().extraHediffs)
                {
                    hediff = pawn.health.hediffSet.hediffs.Find((Hediff x) => x.def == extraHediffDef && x.Part == null /* part */ && x.Visible);
                    if (hediff != null)
                    {
                        pawn.health.RemoveHediff(hediff);
                    }
                }
            }

            DamageDef damageType = DamageDefOf.Bomb;
            int damageDealt = 99999;
            Random random = new Random();
            int injuries = random.Next(1, 6);
            BodyPartRecord bodyPartRecord;
            for (int i = 0; i < injuries; i++)
            {
                bodyPartRecord = GetPart(pawn);
                pawn.TakeDamage(new DamageInfo(damageType, damageDealt, -1f, -1f, billDoer, bodyPartRecord, null, DamageInfo.SourceCategory.ThingOrUnknown));
                if(bodyPartRecord.Label.Equals("Brain"))
                {
                    i = 10000000;
                }
            }
            //pawn.health.RemoveHediff(AddictionUtility.FindAddictionHediff(pawn, DefDatabase<ChemicalDef>.GetNamed("Luciferium")));
        }

        public override string GetLabelWhenUsedOn(Pawn pawn, BodyPartRecord part)
        {
            /*
            if (pawn.RaceProps.IsMechanoid || pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(part))
            {
                return RecipeDefOf.RemoveBodyPart.LabelCap;
            }
            BodyPartRemovalIntent bodyPartRemovalIntent = HealthUtility.PartRemovalIntent(pawn, part);
            if (bodyPartRemovalIntent != BodyPartRemovalIntent.Amputate)
            {
                if (bodyPartRemovalIntent != BodyPartRemovalIntent.Harvest)
                {
                    throw new InvalidOperationException();
                }
                return "Harvest".Translate();
            }
            else
            {
                if (part.depth == BodyPartDepth.Inside || part.def.)
                {
                    return "RemoveOrgan".Translate();
                }
                return "Amputate".Translate();
            }
            */
            return "Remove Luciferium Addiction";
        }
    }
}
