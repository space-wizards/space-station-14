using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.ActionBlocker;
using Content.Shared.Atmos.Piping;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems
{
    [UsedImplicitly]
    public class GasValveSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasValveComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<GasValveComponent, ActivateInWorldEvent>(OnActivate);
        }

        private void OnStartup(EntityUid uid, GasValveComponent component, ComponentStartup args)
        {
            // We call set in startup so it sets the appearance, node state, etc.
            Set(uid, component, component.Open);
        }

        private void OnActivate(EntityUid uid, GasValveComponent component, ActivateInWorldEvent args)
        {
            if(args.User.InRangeUnobstructed(args.Target) && Get<ActionBlockerSystem>().CanInteract(args.User))
                Toggle(uid, component);
        }

        public void Set(EntityUid uid, GasValveComponent component, bool value)
        {
            component.Open = value;

            if (EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
                && nodeContainer.TryGetNode(component.PipeName, out PipeNode? pipe))
            {
                pipe.ConnectionsEnabled = component.Open;
            }
        }

        public void Toggle(EntityUid uid, GasValveComponent component)
        {
            Set(uid, component, !component.Open);
        }
    }
}
