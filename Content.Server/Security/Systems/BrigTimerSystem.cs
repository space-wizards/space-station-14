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
using Robust.Shared.Timing;

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
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BrigTimerComponent, ActivateInWorldEvent>(OnActivate);
        }

        private void OnActivate(EntityUid uid, BrigTimerComponent component, ActivateInWorldEvent args)
        {
            component.TriggerTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.Delay);
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

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            foreach (var timer in EntityQuery<BrigTimerComponent>())
            {
                if (!timer.Activated)
                    continue;

                if (timer.TriggerTime <= _gameTiming.CurTime)
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
