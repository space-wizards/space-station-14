using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Shuttles
{
    [NetworkedComponent]
    public abstract class SharedDockingComponent : Component
    {
        public override string Name => "Docking";

        [ViewVariables]
        public bool Enabled = false;

        public abstract bool Docked { get; }
    }
}
