using Verse;

namespace ExpandableFermenting
{
    class CompProperties_Fermenting : CompProperties_Processing
    {
        public const float DefaultMinIdealTemperature = 7.0f;
        public float minIdealTemperature = DefaultMinIdealTemperature; // Progress reduced below this temperature.
        public string outOfIdealTemperatureLabel = "Non-ideal temperature. Fermentation speed:";

        public CompProperties_Fermenting() : base()
        {
            processFinishedLabel = "Fermented";
            idealTempLabel = "Ideal fermenting temperature";
        }
    }
}
