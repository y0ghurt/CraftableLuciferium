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
            //BodyPartDef partDef;
            Random random = new Random();
            List<BodyPartDef> partDefs = new List<BodyPartDef>();
            partDefs.Add(BodyPartDefOf.Leg);
            partDefs.Add(BodyPartDefOf.Hand);
            partDefs.Add(BodyPartDefOf.Arm);
            partDefs.Add(BodyPartDefOf.Eye);
            partDefs.Add(BodyPartDefOf.Shoulder);

            for (int retries = 0; retries < 16; retries++)
            {
                //bool firstRun = true;
                //partDef = new BodyPartDef();
                IEnumerable<BodyPartRecord> bodyPartRecordList = pawn.health.hediffSet.GetNotMissingParts();
                List<BodyPartRecord> selectedBodyParts = new List<BodyPartRecord>();
                foreach (BodyPartRecord record in bodyPartRecordList)
                {
                    foreach (BodyPartDef partDef in partDefs)
                    {
                        if (record.def.Equals(partDef))
                        {
                            selectedBodyParts.Add(record);
                            //   return record;
                        }
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

            BodyPartRecord bodyPartRecord;
            DamageDef damageType = DamageDefOf.Bomb;
            Random random = new Random();
            float damageDealt = 99999;
            int injuries = random.Next(1, 3);
            for (int i = 0; i < injuries; i++)
            {
                if (!pawn.health.Dead)
                {
                    bodyPartRecord = GetPart(pawn);
                    //Log.Message(bodyPartRecord.Label);
                    DamageInfo damageInfo = new DamageInfo(damageType, damageDealt, 0f, -1f, billDoer, bodyPartRecord, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
                    pawn.TakeDamage(damageInfo);
                    if (pawn.health.hediffSet.GetBrain() == null || bodyPartRecord.Label.Equals("brain"))
                    {
                        i = 10000000;
                        break;
                    }
                }
                else
                {
                    //Log.Message("In the else");
                    break;
                }
            }
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
