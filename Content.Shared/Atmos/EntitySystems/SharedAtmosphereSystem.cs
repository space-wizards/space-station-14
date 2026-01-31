using Content.Shared.Atmos.Prototypes;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Atmos.EntitySystems
{
    public abstract partial class SharedAtmosphereSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedInternalsSystem _internals = default!;

        private EntityQuery<InternalsComponent> _internalsQuery;

        public string?[] GasReagents = new string[Atmospherics.TotalNumberOfGases];

        protected readonly GasPrototype[] GasPrototypes = new GasPrototype[Atmospherics.TotalNumberOfGases];

        public override void Initialize()
        {
            base.Initialize();

            _internalsQuery = GetEntityQuery<InternalsComponent>();

            InitializeBreathTool();

            foreach (var gas in Enum.GetValues<Gas>())
            {
                var idx = (int)gas;
                // Log an error if the corresponding prototype isn't found
                if (!_prototypeManager.TryIndex<GasPrototype>(gas.ToString(), out var gasPrototype))
                {
                    Log.Error($"Failed to find corresponding {nameof(GasPrototype)} for gas ID {(int)gas} ({gas}) with expected ID \"{gas.ToString()}\". Is your prototype named correctly?");
                    continue;
                }
                GasPrototypes[idx] = gasPrototype;
                GasReagents[idx] = gasPrototype.Reagent;
            }
        }

        public GasPrototype GetGas(int gasId) => GasPrototypes[gasId];

        public GasPrototype GetGas(Gas gasId) => GasPrototypes[(int) gasId];

        public IEnumerable<GasPrototype> Gases => GasPrototypes;
    }
}
