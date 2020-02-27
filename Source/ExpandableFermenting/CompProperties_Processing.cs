using Verse;

namespace Ceramics
{
    class CompProperties_Processing : CompProperties
    {
        public ThingDef ingredientDef = null;
        public ThingFilter ingredientFilter = null;
        public string ingredientLabel = null;
        public ThingDef productDef = null;
        public int capacity = 25;
        public int processingDuration = 360000; // 6 days
        public string processFinishedLabel = "Finished";
        public string idealTempLabel = "Ideal processing temperature";

        public CompProperties_Processing()
        {
            this.compClass = typeof(CompProcessing);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            if (ingredientFilter != null) ingredientFilter.ResolveReferences();
        }
    }
}
