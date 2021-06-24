using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Alerts
{
    internal class ClientAlertsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ClientAlertsComponent, PlayerAttachedEvent>((_, component, _) => component.PlayerAttached());
            SubscribeLocalEvent<ClientAlertsComponent, PlayerDetachedEvent>((_, component, _) => component.PlayerDetached());
        }
    }
}
