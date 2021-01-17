using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Ceramics
{

    [DefOf]
    public static class N7DefsOf
    {
        public static JobDef UnloadProcessingBuilding;
        public static JobDef LoadProcessingBuilding;
        static N7DefsOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf));
        }
    }

    class WorkGiver_TakeProductOutOfProcessingBuilding : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => Building_Processing.processors;
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            return thing is Building_Processing building && building.Completed && !thing.IsBurning() && !thing.IsForbidden(pawn) && pawn.CanReserve(thing, 1, -1, null, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            return new Job(N7DefsOf.UnloadProcessingBuilding, thing);
        }
    }

}
