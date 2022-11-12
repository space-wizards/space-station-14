using Robust.Client.GameObjects;

namespace Content.Client.Suspicion
{
    sealed class SuspicionRoleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SuspicionRoleComponent, ComponentAdd>((_, component, _) => component.AddUI());
            SubscribeLocalEvent<SuspicionRoleComponent, ComponentRemove>((_, component, _) => component.RemoveUI());

            SubscribeLocalEvent<SuspicionRoleComponent, PlayerAttachedEvent>((_, component, _) => component.AddUI());
            SubscribeLocalEvent<SuspicionRoleComponent, PlayerDetachedEvent>((_, component, _) => component.RemoveUI());
        }
    }
}
