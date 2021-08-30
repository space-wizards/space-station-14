using Content.Server.PDA.Managers;
using Content.Server.Traitor.Uplink.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Traitor.Uplink.Systems
{
    public class UplinkSystem : EntitySystem
    {
        [Dependency] private readonly IUplinkManager _uplinkManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UplinkComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<UplinkComponent, ComponentRemove>(OnRemove);
        }

        private void OnInit(EntityUid uid, UplinkComponent component, ComponentInit args)
        {
            RaiseLocalEvent(uid, new UplinkInitEvent(component));
        }

        private void OnRemove(EntityUid uid, UplinkComponent component, ComponentRemove args)
        {
            RaiseLocalEvent(uid, new UplinkRemovedEvent());
        }
    }
}
