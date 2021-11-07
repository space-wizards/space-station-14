using Content.Shared.Shuttles;
using Robust.Shared.GameObjects;

namespace Content.Client.Shuttles
{
    [RegisterComponent]
    public sealed class DockingComponent : SharedDockingComponent
    {
        public override bool Docked => _docked;

        private bool _docked;
    }
}
