using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Suspicion
{
    sealed class SuspicionRoleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SuspicionRoleComponent, PlayerAttachedEvent>((_, component, _) => component.PlayerAttached());
            SubscribeLocalEvent<SuspicionRoleComponent, PlayerDetachedEvent>((_, component, _) => component.PlayerDetached());
        }
    }
}
