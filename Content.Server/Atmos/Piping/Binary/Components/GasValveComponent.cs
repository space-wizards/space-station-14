using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.NodeContainer;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Binary.Components
{
    // TODO ATMOS: Make ECS.
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
            if(eventArgs.InRangeUnobstructed() && EntitySystem.Get<ActionBlockerSystem>().CanInteract(eventArgs.User))
                Toggle();
        }
    }
}
