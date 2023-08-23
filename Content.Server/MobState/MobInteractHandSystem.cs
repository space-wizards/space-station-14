using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Interaction;
using Content.Shared.Bed.Sleep;
using Content.Shared.Stunnable;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Bed.Sleep;
using Content.Server.Interaction;
using Content.Server.Interaction.Components;

namespace Content.Server.Mobs
{
    /// <summary>
    /// Handles interact hand events on mobs.
    /// </summary>
    public sealed class NyanoMobInteractHandSystem : EntitySystem
    {
        [Dependency] private readonly SleepingSystem _sleepingSystem = default!;
        [Dependency] private readonly RespiratorSystem _respirator = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MobStateComponent, InteractHandEvent>(OnInteractHand);
        }

        /// <summary>
        /// Handle CPR, helping/waking, and hugging.
        /// Why not do it as an event?
        /// 1. These have a clear order of priority.
        /// 2. I do not forsee this list expanding much if it all. The benefit of avoding conflicting behavior
        ///    outweighs the incredibly unlikely possibility there are ever more than like 5 or 6 of these.
        /// </summary>
        private void OnInteractHand(EntityUid uid, MobStateComponent component, InteractHandEvent args)
        {
            // Highest to lowest prio:

            // 1. Are we in crit and suffocating?
            if (component.CurrentState == Shared.Mobs.MobState.Critical && TryComp<RespiratorComponent>(uid, out var respirator))
            {
                _respirator.AttemptCPR(uid, respirator, args.User);
                args.Handled = true;
                return;
            }

            // 2. Are we sleeping?
            if (TryComp<SleepingComponent>(uid, out var sleeping))
            {
                _sleepingSystem.WakeWithHands(uid, sleeping, args.User);
                args.Handled = true;
                return;
            }
        }
    }
}
