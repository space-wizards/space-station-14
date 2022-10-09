using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Explosion.Components;
using Content.Server.Flash;
using Content.Server.Flash.Components;
using Content.Server.Sticky.Events;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Content.Shared.Trigger;
using Content.Shared.Database;
using Content.Shared.Explosion;
using Content.Shared.Interaction;
using Content.Shared.Payload.Components;
using Content.Shared.StepTrigger.Systems;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Content.Server.Security.Components;

namespace Content.Server.Security.Systems
{
    /// <summary>
    /// Raised whenever something is Triggered on the entity.
    /// </summary>
    public sealed class TriggerEvent : HandledEntityEventArgs
    {
        public EntityUid Triggered { get; }
        public EntityUid? User { get; }

        public TriggerEvent(EntityUid triggered, EntityUid? user = null)
        {
            Triggered = triggered;
            User = user;
        }
    }

    [UsedImplicitly]
    public sealed partial class BrigTimerSystem : EntitySystem
    {
        [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BrigTimerComponent, ActivateInWorldEvent>(OnActivate);
        }

        private void OnActivate(EntityUid uid, BrigTimerComponent component, ActivateInWorldEvent args)
        {
            component.TimeRemaining = component.Delay;
            component.User = args.User;
            component.Activated = true;

            args.Handled = true;
        }

        public bool Trigger(BrigTimerComponent brigTimer)
        {
            // TODO: Trigger here!

            brigTimer.Activated = false;

            // TODO: Send to linked devices

            return true;
        }
        /*
        public void HandleTimerTrigger(EntityUid uid, EntityUid? user, float delay , float beepInterval, float? initialBeepDelay, SoundSpecifier? beepSound, AudioParams beepParams)
        {
            if (delay <= 0)
            {
                RemComp<ActiveTimerTriggerComponent>(uid);
                Trigger();
                return;
            }

            if (HasComp<ActiveTimerTriggerComponent>(uid))
                return;

            if (user != null)
            {
                // Check if entity is bomb/mod. grenade/etc
                if (_container.TryGetContainer(uid, "payload", out IContainer? container) &&
                    container.ContainedEntities.Count > 0 &&
                    TryComp(container.ContainedEntities[0], out ChemicalPayloadComponent? chemicalPayloadComponent))
                {
                    // If a beaker is missing, the entity won't explode, so no reason to log it
                    if (!TryComp(chemicalPayloadComponent?.BeakerSlotA.Item, out SolutionContainerManagerComponent? beakerA) ||
                        !TryComp(chemicalPayloadComponent?.BeakerSlotB.Item, out SolutionContainerManagerComponent? beakerB))
                        return;

                    _adminLogger.Add(LogType.Trigger,
                        $"{ToPrettyString(user.Value):user} started a {delay} second timer trigger on entity {ToPrettyString(uid):timer}, which contains [{string.Join(", ", beakerA.Solutions.Values.First())}] in one beaker and [{string.Join(", ", beakerB.Solutions.Values.First())}] in the other.");
                }
                else
                {
                    _adminLogger.Add(LogType.Trigger,
                        $"{ToPrettyString(user.Value):user} started a {delay} second timer trigger on entity {ToPrettyString(uid):timer}");
                }

            }
            else
            {
                _adminLogger.Add(LogType.Trigger,
                    $"{delay} second timer trigger started on entity {ToPrettyString(uid):timer}");
            }

            var active = AddComp<ActiveTimerTriggerComponent>(uid);
            active.TimeRemaining = delay;
            active.User = user;
            active.BeepParams = beepParams;
            active.BeepSound = beepSound;
            active.BeepInterval = beepInterval;
            active.TimeUntilBeep = initialBeepDelay == null ? active.BeepInterval : initialBeepDelay.Value;

            if (TryComp<AppearanceComponent>(uid, out var appearance))
                appearance.SetData(TriggerVisuals.VisualState, TriggerVisualState.Primed);
        }
        */

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdateTimer(frameTime);
        }

        private void UpdateTimer(float frameTime)
        {
            foreach (var timer in EntityQuery<BrigTimerComponent>())
            {
                if (!timer.Activated)
                    continue;

                timer.TimeRemaining -= frameTime;

                if (timer.TimeRemaining <= 0)
                {
                    Trigger(timer);

                    if (timer.DoneSound != null)
                    {
                        var filter = Filter.Pvs(timer.Owner, entityManager: EntityManager);
                        _audio.Play(timer.DoneSound, filter, timer.Owner, timer.BeepParams);
                    }
                }
            }
        }
    }
}
