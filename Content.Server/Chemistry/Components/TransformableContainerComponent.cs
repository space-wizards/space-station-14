using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public sealed class TransformableContainerComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public string InitialName = default!;
        public string InitialDescription = default!;
        public ReagentPrototype? CurrentReagent;

        public bool Transformed { get; internal set; }

        protected override void Initialize()
        {
            base.Initialize();

            InitialName = _entMan.GetComponent<MetaDataComponent>(Owner).EntityName;
            InitialDescription = _entMan.GetComponent<MetaDataComponent>(Owner).EntityDescription;
        }

        protected override void Startup()
        {
            base.Startup();

            Owner.EnsureComponentWarn<SolutionContainerManagerComponent>();
            Owner.EnsureComponentWarn<FitsInDispenserComponent>();
        }
    }
}
