using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public sealed class TransformableContainerComponent : Component
    {
        public string InitialName = default!;
        public string InitialDescription = default!;

        public ReagentPrototype? CurrentReagent;
        public bool Transformed { get; internal set; }
    }
}
