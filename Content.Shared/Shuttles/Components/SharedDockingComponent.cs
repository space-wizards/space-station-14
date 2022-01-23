using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Shuttles.Components
{
    public abstract class SharedDockingComponent : Component
    {
        // Yes I left this in for now because there's no overhead and we'll need a client one later anyway
        // and I was too lazy to delete it.
        public override string Name => "Docking";

        [ViewVariables]
        public bool Enabled = false;

        public abstract bool Docked { get; }
    }
}
