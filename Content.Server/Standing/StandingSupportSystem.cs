using Content.Shared.Standing;

namespace Content.Server.Standing
{
    public sealed class StandingSupportSystem : SharedStandingSupportSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<NeedsSupportComponent, ComponentStartup>(OnStartup);
        }

        private void OnStartup(EntityUid uid, NeedsSupportComponent component, ComponentStartup args)
        {
            if (!TryComp<StandingStateComponent>(uid, out var standingState) ||
                IsSupported(uid, component))
                return;
           
           StandingStateSystem.Down(uid, standingState: standingState);
        }
    }
}
