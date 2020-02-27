using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;

namespace Ceramics
{
    class JobDriver_TakeProductOutOfProcessingBuilding : JobDriver
    {
        private const TargetIndex BuildingInd = TargetIndex.A;
        private const TargetIndex ProductInd = TargetIndex.B;
        private const TargetIndex StorageCellInd = TargetIndex.C;
        private const int Duration = 200;

        protected Building_Processing Building => (Building_Processing)job.GetTarget(BuildingInd).Thing;

        protected Thing Product => job.GetTarget(ProductInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Building, job, 1, -1, null, errorOnFailed);
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(BuildingInd);
            this.FailOnBurningImmobile(BuildingInd);
            yield return Toils_Goto.GotoThing(BuildingInd, PathEndMode.Touch);
            yield return Toils_General.Wait(Duration).FailOnDestroyedNullOrForbidden(BuildingInd).FailOnCannotTouch(BuildingInd, PathEndMode.Touch).FailOn(() => !Building.Completed).WithProgressBarToilDelay(BuildingInd);
            yield return new Toil
            {
                initAction = delegate
                {
                    Thing product = Building.TakeOutProduct();
                    GenPlace.TryPlaceThing(product, pawn.Position, Map, ThingPlaceMode.Near);
                    StoragePriority currentPriority = StoreUtility.CurrentStoragePriorityOf(product);
                    if (StoreUtility.TryFindBestBetterStoreCellFor(product, pawn, Map, currentPriority, pawn.Faction, out IntVec3 cell))
                    {
                        job.SetTarget(StorageCellInd, cell);
                        job.SetTarget(ProductInd, product);
                        job.count = product.stackCount;
                    }
                    else
                    {
                        EndJobWith(JobCondition.Incompletable);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return Toils_Reserve.Reserve(ProductInd);
            yield return Toils_Reserve.Reserve(StorageCellInd);
            yield return Toils_Goto.GotoThing(ProductInd, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(ProductInd);
            Toil carryToCell = Toils_Haul.CarryHauledThingToCell(StorageCellInd);
            yield return carryToCell;
            yield return Toils_Haul.PlaceHauledThingInCell(StorageCellInd, carryToCell, true);
            yield break;
        }
    }
}
