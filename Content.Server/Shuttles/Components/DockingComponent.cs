using Content.Shared.Shuttles.Components;
using Robust.Shared.Physics.Dynamics.Joints;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent]
    public sealed class DockingComponent : SharedDockingComponent
    {
        [ViewVariables]
        [DataField("dockedWith")]
        public EntityUid? DockedWith;

        [ViewVariables]
        public Joint? DockJoint;

        [ViewVariables]
        public override bool Docked => DockedWith != null;
    }
}
