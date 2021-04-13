using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Binary
{
    [ComponentReference(typeof(IActivate))]
    [RegisterComponent]
    public class GasValveComponent : Component, IActivate
    {
        public override string Name => "GasValve";

        [ViewVariables]
        [DataField("open")]
        private bool _open = true;

        [DataField("pipe")]
        [ViewVariables(VVAccess.ReadWrite)]
        private string _pipeName = "pipe";

        protected override void Startup()
        {
            base.Startup();

            Set();
        }

        private void Set()
        {
            if (Owner.TryGetComponent(out NodeContainerComponent? nodeContainer)
                && nodeContainer.TryGetNode(_pipeName, out PipeNode? pipe))
            {
                pipe.ConnectionsEnabled = _open;
            }
        }

        private void Toggle()
        {
            _open = !_open;
            Set();
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if(eventArgs.InRangeUnobstructed() && eventArgs.User.CanInteract())
                Toggle();
        }
    }
}
