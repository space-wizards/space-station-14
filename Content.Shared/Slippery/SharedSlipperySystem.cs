using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.StatusEffect;
using Content.Shared.StepTrigger;
using Content.Shared.Stunnable;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Shared.Slippery
{
    [UsedImplicitly]
    public abstract class SharedSlipperySystem : EntitySystem
    {
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedStunSystem _stunSystem = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SlipperyComponent, StepTriggerAttemptEvent>(HandleAttemptCollide);
            SubscribeLocalEvent<SlipperyComponent, StepTriggeredEvent>(HandleStepTrigger);
            SubscribeLocalEvent<NoSlipComponent, SlipAttemptEvent>(OnNoSlipAttempt);
        }

        private void HandleStepTrigger(EntityUid uid, SlipperyComponent component, ref StepTriggeredEvent args)
        {
            TrySlip(component, args.Tripper);
        }

        private void HandleAttemptCollide(
            EntityUid uid,
            SlipperyComponent component,
            ref StepTriggerAttemptEvent args)
        {
            args.Continue |= CanSlip(uid, args.Tripper);
        }

        private static void OnNoSlipAttempt(EntityUid uid, NoSlipComponent component, SlipAttemptEvent args)
        {
            args.Cancel();
        }

        private bool CanSlip(EntityUid uid, EntityUid toSlip)
        {
            return !_container.IsEntityInContainer(uid)
                   && _statusEffectsSystem.CanApplyEffect(toSlip, "Stun"); //Should be KnockedDown instead?
        }

        private void TrySlip(SlipperyComponent component, EntityUid other)
        {
            if (HasComp<KnockedDownComponent>(other))
                return;

            var ev = new SlipAttemptEvent();
            RaiseLocalEvent(other, ev, false);
            if (ev.Cancelled)
                return;

            if (TryComp(other, out PhysicsComponent? physics))
                physics.LinearVelocity *= component.LaunchForwardsMultiplier;

            var playSound = !_statusEffectsSystem.HasStatusEffect(other, "KnockedDown");

            _stunSystem.TryParalyze(other, TimeSpan.FromSeconds(component.ParalyzeTime), true);

            // Preventing from playing the slip sound when you are already knocked down.
            if (playSound)
                PlaySound(component);

            _adminLogger.Add(LogType.Slip, LogImpact.Low,
                $"{ToPrettyString(other):mob} slipped on collision with {ToPrettyString(component.Owner):entity}");
        }

        // Until we get predicted slip sounds TM?
        protected abstract void PlaySound(SlipperyComponent component);
    }

    /// <summary>
    ///     Raised on an entity to determine if it can slip or not.
    /// </summary>
    public sealed class SlipAttemptEvent : CancellableEntityEventArgs, IInventoryRelayEvent
    {
        public SlotFlags TargetSlots { get; } = SlotFlags.FEET;
    }
}
