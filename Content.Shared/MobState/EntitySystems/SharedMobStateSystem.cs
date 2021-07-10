using Content.Shared.MobState.Components;
using Content.Shared.Pulling.Events;
using Robust.Shared.GameObjects;

namespace Content.Shared.MobState.EntitySystems
{
    public class SharedMobStateSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedMobStateComponent, StartPullAttemptEvent>(OnStartPullAttempt);
        }

        private void OnStartPullAttempt(EntityUid uid, SharedMobStateComponent component, StartPullAttemptEvent args)
        {
            if(component.IsIncapacitated())
                args.Cancel();
        }
    }
}
