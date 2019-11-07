using Content.Server.GameObjects.Components.Chemistry;
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

        private SolutionComponent _stomachContents;
        public float MetaboliseDelay => _metaboliseDelay;
        [ViewVariables]
        private float _metaboliseDelay; // How long between metabolisation for 5 units

        public int MaxVolume
        {
            get => _stomachContents.MaxVolume;
            set => _stomachContents.MaxVolume = value;
        }

        private float _metabolisationCounter = 0.0f;

        private int _initialMaxVolume;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _metaboliseDelay, "metabolise_delay", 6.0f);
            serializer.DataField(ref _initialMaxVolume, "max_volume", 20);
        }

        public override void Initialize()
        {
            base.Initialize();
            // Shouldn't add to Owner to avoid cross-contamination (e.g. with blood or whatever they made hold other solutions)
            _stomachContents = new SolutionComponent();
            _stomachContents.InitializeFromPrototype();
            _stomachContents.MaxVolume = _initialMaxVolume;
        }

        public bool TryTransferSolution(Solution solution)
        {
            // TODO: For now no partial transfers. Potentially change by design
            if (solution.TotalVolume + _stomachContents.CurrentVolume > _stomachContents.MaxVolume)
            {
                return false;
            }
            _stomachContents.TryAddSolution(solution, false, true);
            return true;
        }

        /// <summary>
        /// This is where the magic happens. Make people throw up, increase nutrition, whatever
        /// </summary>
        /// <param name="solution"></param>
        public void React(Solution solution)
        {
            // TODO: Implement metabolism post from here
            // https://github.com/space-wizards/space-station-14/issues/170#issuecomment-481835623 as raised by moneyl
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
            if (_stomachContents.CurrentVolume == 0)
            {
                return;
            }

            var metabolisation = _stomachContents.SplitSolution(MetabolisationAmount);

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
