using Content.Shared.Atmos.Components;
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

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                GasPrototypes[i] = _prototypeManager.Index<GasPrototype>(i.ToString());
                GasReagents[i] = GasPrototypes[i].Reagent;
            }
        }

        public GasPrototype GetGas(int gasId) => GasPrototypes[gasId];

        public GasPrototype GetGas(Gas gasId) => GasPrototypes[(int) gasId];

        public IEnumerable<GasPrototype> Gases => GasPrototypes;
    }
}
