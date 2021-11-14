using Content.Shared.Shuttles;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.ViewVariables;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent]
    public sealed class DockingComponent : SharedDockingComponent
    {
        [ViewVariables]
        public DockingComponent? DockedWith;

        [ViewVariables]
        public Joint? DockJoint;

        [ViewVariables]
        public override bool Docked => DockedWith != null;
    }
}
