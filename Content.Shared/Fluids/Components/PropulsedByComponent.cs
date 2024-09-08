using Robust.Shared.GameStates;

namespace Content.Shared.Fluids.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class PropulsedByComponent : Component
    {
        public HashSet<Entity<PropulsionComponent>> Sources;

        public PropulsedByComponent()
        {
            Sources = new HashSet<Entity<PropulsionComponent>>();
        }
    }
}