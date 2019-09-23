using System;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Nutrition;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public class StomachComponent : SharedStomachComponent
    {
        // Essentially every time it ticks it'll pull out the MetabolisationAmount of reagents and process them.
        // Generic food goes under "nutriment" like SS13
        // There's also separate hunger and thirst components which means you can have a stomach
        // but not require food / water.
        public static readonly int NutrimentFactor = 30;
        public static readonly int HydrationFactor = 30;
        public static readonly int MetabolisationAmount = 5;

        public Solution StomachContents => _stomachContents;
        [ViewVariables]
        private Solution _stomachContents = new Solution();
        public int MaxVolume => _maxVolume;
        [ViewVariables]
        private int _maxVolume;
        public float MetaboliseDelay => _metaboliseDelay;
        [ViewVariables]
        private float _metaboliseDelay; // How long between metabolisation for 5 units


        private float _metabolisationCounter = 0.0f;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _maxVolume, "max_volume", 20);
            serializer.DataField(ref _metaboliseDelay, "metabolise_delay", 6.0f);
        }

        public bool TryTransferSolution(Solution solution)
        {
            // TODO: For now no partial transfers. Potentially change by design
            if (solution.TotalVolume + StomachContents.TotalVolume > MaxVolume)
            {
                return false;
            }
            StomachContents.AddSolution(solution);
            return true;
        }

        /// <summary>
        /// This is where the magic happens. Make people throw up, increase nutrition, whatever
        /// </summary>
        /// <param name="solution"></param>
        public void React(Solution solution)
        {
            // TODO: Optimise further?
            var hungerUpdate = 0;
            var thirstUpdate = 0;
            foreach (var reagent in solution.Contents)
            {
                switch (reagent.ReagentId)
                {
                    case "chem.Nutriment":
                        hungerUpdate++;
                        break;
                    case "chem.H2O":
                        thirstUpdate++;
                        break;
                    case "chem.Alcohol":
                        thirstUpdate++;
                        break;
                    default:
                        continue;
                }
            }

            // Quantity x restore amount per unit
            if (hungerUpdate > 0 && Owner.TryGetComponent(out HungerComponent hungerComponent))
            {
                hungerComponent.UpdateFood(hungerUpdate * NutrimentFactor);
            }

            if (thirstUpdate > 0 && Owner.TryGetComponent(out ThirstComponent thirstComponent))
            {
                thirstComponent.UpdateThirst(thirstUpdate * HydrationFactor);
            }

            // TODO: Dispose solution?
        }

        public void Metabolise()
        {
            if (StomachContents.TotalVolume == 0)
            {
                return;
            }

            var metabolisation = StomachContents.SplitSolution(MetabolisationAmount);

            React(metabolisation);
        }

        public void OnUpdate(float frameTime)
        {
            _metabolisationCounter += frameTime;
            if (_metabolisationCounter >= MetaboliseDelay)
            {
                // Going to be rounding issues with frametime but no easy way to avoid it with int reagents.
                // It is a long-term mechanic so shouldn't be a big deal.
                Metabolise();
                _metabolisationCounter -= MetaboliseDelay;
            }
        }
    }
}
