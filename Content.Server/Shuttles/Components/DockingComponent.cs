using Content.Shared.Shuttles.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

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
