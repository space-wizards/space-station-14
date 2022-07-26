using Robust.Shared.Timing;

namespace Content.Server.AI.Tracking
{
    public sealed class RecentlyInjectedSystem : EntitySystem
    {
        [Dependency] private readonly TimedEventSystem _timedEventSystem = default!;
        private const string InjectExpiredKey = nameof(InjectExpiredKey);
        public override void Initialize()
        {
            SubscribeLocalEvent<RecentlyInjectedComponent, ComponentInit>(OnInjectInit);
            SubscribeLocalEvent<RecentlyInjectedComponent, ComponentTimedEvent>(OnInjectExpired);
        }

        private void OnInjectInit(EntityUid uid, RecentlyInjectedComponent component, ComponentInit args)
        {
            _timedEventSystem.AddTimedEvent(component, component.RemoveTime, InjectExpiredKey);
        }

        private void OnInjectExpired(EntityUid uid, RecentlyInjectedComponent component, ComponentTimedEvent args)
        {
            RemComp<RecentlyInjectedComponent>(uid);
        }
    }
}
