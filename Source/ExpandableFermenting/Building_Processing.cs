using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Verse;

namespace Ceramics
{
    [StaticConstructorOnStartup]
    class Building_Processing : Building
    {
        private const float progressAtCompleted = 1f;
        private static readonly Vector2 BarSize = new Vector2(0.55f, 0.1f);
        private static readonly Color BarZeroProgressColor = new Color(0.4f, 0.27f, 0.22f);
        private static readonly Color BarFermentedColor = new Color(0.9f, 0.85f, 0.2f);
        private static readonly Material BarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f), false);

        public int contentsCount;
        public float progress;

        private Material barFilledCachedMat;

        public int Capacity => CompProcessing.Capacity;
        public CompProcessing CompProcessing => GetComp<CompProcessing>();
        public bool Empty => contentsCount <= 0;
        public int EstimatedTicksLeft
        {
            get
            {
                return Mathf.Max(Mathf.RoundToInt((progressAtCompleted - Progress) / GetProgressPerTickNow()), 0);
            }
        }
        public int ProcessingDuration => CompProcessing.ProcessingDuration;
        public bool Completed => !Empty && Progress >= 1f;
        public string ProcessedLabel => CompProcessing.ProcessedLabel;
        public int SpaceLeft => Completed ? 0 : Capacity - contentsCount;

        public float Progress
        {
            get
            {
                return progress;
            }
            set
            {
                if (value != progress)
                {
                    progress = value;
                    barFilledCachedMat = null;
                }
            }
        }

        public virtual bool UsableNow
        {
            get
            {
                CompPowerTrader powerComp = GetComp<CompPowerTrader>();
                CompRefuelable refuelableComp = GetComp<CompRefuelable>();
                CompBreakdownable breakdownableComp = GetComp<CompBreakdownable>();
                return ((powerComp == null || powerComp.PowerOn)) && (refuelableComp == null || refuelableComp.HasFuel) && (breakdownableComp == null || !breakdownableComp.BrokenDown);
            }
        }

        private Material BarFilledMat
        {
            get
            {
                if (barFilledCachedMat == null)
                {
                    barFilledCachedMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.Lerp(BarZeroProgressColor, BarFermentedColor, Progress), false);
                }
                return barFilledCachedMat;
            }
        }

        public virtual float GetProgressPerTickNow()
        {
            if (UsableNow)
            {
                return 1f / ProcessingDuration;
            }
            else
            {
                return 0f;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref contentsCount, "contentsCount", 0, false);
            Scribe_Values.Look(ref progress, "progress", 0f, false);
        }

        public override void Tick()
        {
            base.Tick();
            if (!Empty)
            {
                Progress = Mathf.Min(Progress + GetProgressPerTickNow(), 1f);
            }
        }

        public override void TickRare()
        {
            base.TickRare();
            if (!Empty)
            {
                Progress = Mathf.Min(Progress + 250f * GetProgressPerTickNow(), 1f);
            }
        }

        public override void TickLong()
        {
            base.TickLong();
            if (!Empty)
            {
                Progress = Mathf.Min(Progress + 2000f * GetProgressPerTickNow(), 1f);
            }
        }

        public void AddIngredient(int count)
        {
            CompTemperatureRuinable temperatureRuinable = GetComp<CompTemperatureRuinable>();
            if (temperatureRuinable != null)
            {
                temperatureRuinable.Reset();
            }
            if (Completed)
            {
                Log.Warning(string.Format("Tried to add {0} to a building full of {1}. Colonists should take the {1} first.", CompProcessing.IngredientLabel, CompProcessing.ProductDef.label));
                return;
            }
            int added = Mathf.Min(count, Capacity - contentsCount);
            if (added > 0)
            {
                Progress = GenMath.WeightedAverage(0f, added, Progress, contentsCount);
                contentsCount += added;
            }
        }

        public void AddIngredient(Thing ingredient)
        {
            int added = Mathf.Min(ingredient.stackCount, Capacity - contentsCount);
            if (added > 0)
            {
                AddIngredient(added);
                ingredient.SplitOff(added).Destroy(DestroyMode.Vanish);
            }
        }

        protected override void ReceiveCompSignal(string signal)
        {
            if (signal == "RuinedByTemperature")
            {
                Reset();
            }
        }

        private void Reset()
        {
            contentsCount = 0;
            Progress = 0f;
        }

        public virtual string GetProcessingString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            CompTemperatureRuinable tempRuinable = GetComp<CompTemperatureRuinable>();
            if (!Empty && (tempRuinable == null || !tempRuinable.Ruined))
            {
                string contentsLabel = Completed ? CompProcessing.ProductDef.label : CompProcessing.IngredientLabel;
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
                }
            }
            if (tempRuinable != null)
            {
                stringBuilder.AppendLine("Temperature".Translate() + ": " + AmbientTemperature.ToStringTemperature("F0"));
                stringBuilder.AppendLine(string.Concat(new string[]
                {
                CompProcessing.IdealTempLabel,
                ": ",
                tempRuinable.Props.minSafeTemperature.ToStringTemperature("F0"),
                " ~ ",
                tempRuinable.Props.maxSafeTemperature.ToStringTemperature("F0")
                }));
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            if (stringBuilder.Length != 0)
            {
                stringBuilder.AppendLine();
            }
            stringBuilder.Append(GetProcessingString());
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public Thing TakeOutProduct()
        {
            ThingDef productDef = CompProcessing.ProductDef;
            if (!Completed)
            {
                Log.Warning(string.Format("Tried to get {0} but it's not yet finished.", productDef.label));
                return null;
            }
            ThingDef stuffDef = null;
            if (productDef.MadeFromStuff)
            {
                stuffDef = CompProcessing.contentsDef;
            }
            CompProcessing.contentsDef = null;
            Thing thing = ThingMaker.MakeThing(productDef, stuffDef);
            thing.stackCount = contentsCount;
            Reset();
            return thing;
        }

        public override void Draw()
        {
            base.Draw();
            if (!Empty)
            {
                Vector3 drawPos = DrawPos;
                drawPos.y += 0.046875f;
                drawPos.z += 0.25f;
                GenDraw.DrawFillableBar(new GenDraw.FillableBarRequest
                {
                    center = drawPos,
                    size = BarSize,
                    fillPercent = (float)contentsCount / Capacity,
                    filledMat = BarFilledMat,
                    unfilledMat = BarUnfilledMat,
                    margin = 0.1f,
                    rotation = Rot4.North
                });
            }
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (Prefs.DevMode && !Empty)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Debug: Set progress to 1",
                    action = delegate
                    {
                        Progress = 1f;
                    }
                };
            }
        }
    }
}
