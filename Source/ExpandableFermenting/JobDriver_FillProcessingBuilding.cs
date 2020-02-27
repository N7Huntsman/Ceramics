using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;

namespace Ceramics
{
    class JobDriver_FillProcessingBuilding : JobDriver
    {
        private const TargetIndex BuildingInd = TargetIndex.A;
        private const TargetIndex IngredientInd = TargetIndex.B;
        private const int Duration = 200;

        protected Building_Processing Building => (Building_Processing)job.GetTarget(BuildingInd).Thing;
        protected Thing Ingredient => job.GetTarget(IngredientInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Building, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Ingredient, job, 1, -1, null, errorOnFailed);
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(BuildingInd);
            this.FailOnBurningImmobile(BuildingInd);
            base.AddEndCondition(() => (this.Building.SpaceLeft > 0) ? JobCondition.Ongoing : JobCondition.Succeeded);
            yield return Toils_General.DoAtomic(delegate { this.job.count = this.Building.SpaceLeft; });
            Toil reserveIngredient = Toils_Reserve.Reserve(IngredientInd);
            yield return reserveIngredient;
            yield return Toils_Goto.GotoThing(IngredientInd, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(IngredientInd).FailOnSomeonePhysicallyInteracting(IngredientInd);
            yield return Toils_Haul.StartCarryThing(IngredientInd, false, true).FailOnDestroyedNullOrForbidden(IngredientInd);
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveIngredient, IngredientInd, TargetIndex.None, true);
            yield return Toils_Goto.GotoThing(BuildingInd, PathEndMode.Touch);
            yield return Toils_General.Wait(Duration).FailOnDestroyedNullOrForbidden(IngredientInd).FailOnDestroyedNullOrForbidden(BuildingInd).FailOnCannotTouch(BuildingInd, PathEndMode.Touch).WithProgressBarToilDelay(BuildingInd, false, -0.5f);
            yield return new Toil
            {
                initAction = delegate
                {
                    Building.AddIngredient(Ingredient);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield break;
        }
    }
}
