using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Hands
{
    internal class HandsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandsComponent, PlayerAttachedEvent>((_, component, _) => component.PlayerAttached());
            SubscribeLocalEvent<HandsComponent, PlayerDetachedEvent>((_, component, _) => component.PlayerDetached());
        }
    }
}
