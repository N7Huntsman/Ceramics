using Verse;

namespace Ceramics
{
    internal class CompProcessing : ThingComp
    {

        internal ThingDef contentsDef;

        public CompProperties_Processing Props => props as CompProperties_Processing;
        public ThingDef IngredientDef => Props.ingredientDef;
        public ThingFilter IngredientFilter => Props.ingredientFilter;
        public ThingDef ProductDef => Props.productDef;
        public int Capacity => Props.capacity;
        public int ProcessingDuration => Props.processingDuration;
        public string ProcessedLabel => Props.processFinishedLabel;
        public string IdealTempLabel => Props.idealTempLabel;

        public string IngredientLabel
        {
            get
            {
                if (ProductDef.MadeFromStuff && contentsDef != null)
                {
                    return contentsDef.label;
                }
                else if (Props.ingredientLabel == null)
                {
                    if (Props.ingredientDef == null)
                    {
                        return "ingredient";
                    } else
                    {
                        return Props.ingredientDef.label;
                    }
                }
                else
                {
                    return Props.ingredientLabel;
                }
            }
        }
    }
}