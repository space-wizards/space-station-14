using Robust.Shared.GameStates;

namespace Content.Shared.Fluids.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class PropulsedByComponent : Component
    {
        [ViewVariables(VVAccess.ReadOnly)]
        public HashSet<Entity<PropulsionComponent>> Sources = new();
    }
}