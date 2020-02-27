using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace Ceramics
{

    public class WorkGiver_FillProcessingBuilding : WorkGiver_Scanner
    {

        public static JobDef LoadProcessingBuilding;

        private static string TemperatureTrans;
        private static string NoIngredientTrans;

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public static void ResetStaticData()
        {
            TemperatureTrans = "BadTemperature".Translate().ToLower();
            NoIngredientTrans = "NoFermentingIngredient".Translate();
        }

        public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            Building_Processing building = thing as Building_Processing;
            if (building == null || building.Completed || building.SpaceLeft <= 0)
            {
                return false;
            }
            float ambientTemperature = building.AmbientTemperature;
            CompProperties_TemperatureRuinable compProperties = building.def.GetCompProperties<CompProperties_TemperatureRuinable>();
            if (compProperties != null && (ambientTemperature < compProperties.minSafeTemperature + 2f || ambientTemperature > compProperties.maxSafeTemperature - 2f))
            {
                JobFailReason.Is(TemperatureTrans);
                return false;
            }
            if (thing.IsForbidden(pawn) || !pawn.CanReserve(thing, 1, -1, null, forced))
            {
                return false;
            }
            if (pawn.Map.designationManager.DesignationOn(thing, DesignationDefOf.Deconstruct) != null)
            {
                return false;
            }
            if (FindIngredient(pawn, building) == null)
            {
                JobFailReason.Is(NoIngredientTrans);
                return false;
            }
            return !thing.IsBurning();
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            Building_Processing building = (Building_Processing) thing;
            Thing ingredient = FindIngredient(pawn, building);
            return new Job(N7DefsOf.LoadProcessingBuilding, thing, ingredient);
        }

        private Thing FindIngredient(Pawn pawn, Building_Processing building)
        {
            ThingDef ingredientDef = building.CompProcessing.IngredientDef;
            ThingFilter ingredientFilter = building.CompProcessing.IngredientFilter;
            ThingDef contentsDef = building.CompProcessing.contentsDef;
            Predicate<Thing> ingredientValidator;
            ThingRequest ingredientRequest;

            // Contents for when stuff matters.
            if (building.CompProcessing.ProductDef.MadeFromStuff && contentsDef != null)
            {
                ingredientValidator = (Thing thing) => !thing.IsForbidden(pawn) && pawn.CanReserve(thing);
                ingredientRequest = ThingRequest.ForDef(contentsDef);
            }

            // Use category if no ingredient def.
            else if (ingredientDef == null)
            {
                if (ingredientFilter == null)
                {
                    // Complete failure.
                    return null;
                }
                ingredientValidator = (Thing thing) => !thing.IsForbidden(pawn) && pawn.CanReserve(thing) && ingredientFilter.Allows(thing);
                ingredientRequest = ingredientFilter.BestThingRequest;
            }

            // Single ingredient def if possible.
            else
            {
                ingredientValidator = (Thing thing) => !thing.IsForbidden(pawn) && pawn.CanReserve(thing);
                ingredientRequest = ThingRequest.ForDef(ingredientDef);
            }
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ingredientRequest, PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, ingredientValidator);
        }
    }
}