using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Verse;

namespace ExpandableFermenting
{
    class Building_Fermenting : Building_Processing
    {
        private const float minTempSpeedFactor = 0.1f;
        private const float maxTempSpeedFactor = 1.0f;

        public string OutOfIdealTemperatureLabel => (CompFermenting.Props as CompProperties_Fermenting).outOfIdealTemperatureLabel;

        public float MinIdealTemperature
        {
            get
            {
                CompProperties_Fermenting compProperties = CompFermenting.props as CompProperties_Fermenting;
                if (compProperties == null)
                {
                    return CompProperties_Fermenting.DefaultMinIdealTemperature;
                }
                return compProperties.minIdealTemperature;
            }
        }

        public float CurrentTempProgressSpeedFactor
        {
            get
            {
                CompProperties_TemperatureRuinable tempRuinable_properties = def.GetCompProperties<CompProperties_TemperatureRuinable>();
                float ambientTemperature = AmbientTemperature;

                // If no CompProperties_TemperatureRuinable, just slow progress below ideal temperature.
                if (tempRuinable_properties == null)
                {
                    if (ambientTemperature >= MinIdealTemperature)
                    {
                        return maxTempSpeedFactor;
                    }
                    else
                    {
                        return minTempSpeedFactor;
                    }
                }
                if (ambientTemperature < tempRuinable_properties.minSafeTemperature)
                {
                    return minTempSpeedFactor;
                }
                if (ambientTemperature < MinIdealTemperature)
                {
                    return GenMath.LerpDouble(tempRuinable_properties.minSafeTemperature, MinIdealTemperature, minTempSpeedFactor, maxTempSpeedFactor, ambientTemperature);
                }
                return maxTempSpeedFactor;
            }
        }

        override public float GetProgressPerTickNow()
        {
            return base.GetProgressPerTickNow() * CurrentTempProgressSpeedFactor;
        }

        public override string GetProcessingString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            CompTemperatureRuinable tempRuinable = GetComp<CompTemperatureRuinable>();
            if (!Empty && (tempRuinable == null || !tempRuinable.Ruined))
            {
                string contentsLabel = Completed ? CompFermenting.ProductDef.label : CompFermenting.IngredientLabel;
                stringBuilder.AppendLine("FermentingBuildingContainsSomething".Translate(new object[]
                {
                    contentsCount,
                    Capacity,
                    contentsLabel
                }));
            }
            if (!Empty)
            {
                if (Completed)
                {
                    stringBuilder.AppendLine(ProcessedLabel.Translate());
                }
                else
                {
                    stringBuilder.AppendLine("FermentationProgress".Translate(new object[]
                    {
                        Progress.ToStringPercent(),
                        EstimatedTicksLeft.ToStringTicksToPeriod()
                    }));
                    if (CurrentTempProgressSpeedFactor != 1f)
                    {
                        stringBuilder.Append(OutOfIdealTemperatureLabel);
                        stringBuilder.AppendLine(CurrentTempProgressSpeedFactor.ToStringPercent());
                    }
                }
            }
            stringBuilder.AppendLine("Temperature".Translate() + ": " + AmbientTemperature.ToStringTemperature("F0"));
            if (tempRuinable == null)
            {
                stringBuilder.AppendLine(string.Concat(new string[]
                {
                CompFermenting.IdealTempLabel,
                ": >",
                MinIdealTemperature.ToStringTemperature("F0")}));
            }
            else
            {
                stringBuilder.AppendLine(string.Concat(new string[]
                {
                CompFermenting.IdealTempLabel,
                ": ",
                MinIdealTemperature.ToStringTemperature("F0"),
                " ~ ",
                tempRuinable.Props.maxSafeTemperature.ToStringTemperature("F0")
                }));
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }
    }
}
