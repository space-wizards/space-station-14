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
using Robust.Shared.Timing;
using Content.Server.MachineLinking.Components;
using Content.Shared.TextScreen;
using Robust.Server.GameObjects;
using Content.Shared.MachineLinking;

namespace Content.Server.MachineLinking.System
{

    public sealed partial class SignalTimerSystem : EntitySystem
    {
        [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;


        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SignalTimerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SignalTimerComponent, ActivateInWorldEvent>(OnActivate);
        }

        private void OnInit(EntityUid uid, SignalTimerComponent component, ComponentInit args)
        {
            if (TryComp(uid, out SignalTransmitterComponent? comp))
            {
                comp.Outputs.TryAdd(component.TriggerPort, new());
                comp.Outputs.TryAdd(component.StartPort, new());
            }
        }

        private void OnActivate(EntityUid uid, SignalTimerComponent component, ActivateInWorldEvent args)
        {
            component.TriggerTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.Delay);
            component.User = args.User;
            component.Activated = true;

            _appearanceSystem.SetData(uid, TextScreenVisuals.Mode, TextScreenMode.Timer);
            _appearanceSystem.SetData(uid, TextScreenVisuals.TargetTime, component.TriggerTime);

            _signalSystem.InvokePort(uid, component.StartPort);

            args.Handled = true;
        }

        public bool Trigger(EntityUid uid, SignalTimerComponent signalTimer)
        {
            signalTimer.Activated = false;

            _signalSystem.InvokePort(uid, signalTimer.TriggerPort);

            _appearanceSystem.SetData(uid, TextScreenVisuals.Mode, TextScreenMode.Text);

            return true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            foreach (var timer in EntityQuery<SignalTimerComponent>())
            {
                if (!timer.Activated)
                    continue;

                if (timer.TriggerTime <= _gameTiming.CurTime)
                {
                    Trigger(timer.Owner, timer);

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
