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
using Content.Shared.Disposal.Components;
using Content.Server.UserInterface;
using Content.Server.Access.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;

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
        [Dependency] private readonly UserInterfaceSystem _ui = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SignalTimerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SignalTimerComponent, ActivateInWorldEvent>(OnActivate); //TODO: Remove

            SubscribeLocalEvent<SignalTimerComponent, SignalTimerTextChangedMessage>(OnTextChangedMessage);
            SubscribeLocalEvent<SignalTimerComponent, SignalTimerDelayChangedMessage>(OnDelayChangedMessage);
            SubscribeLocalEvent<SignalTimerComponent, SignalTimerStartMessage>(OnTimerStartMessage);
            SubscribeLocalEvent<SignalTimerComponent, AfterActivatableUIOpenEvent>(AfterUIOpen);
        }

        private void AfterUIOpen(EntityUid uid, SignalTimerComponent component, AfterActivatableUIOpenEvent args)
        {
            if (!_ui.TryGetUi(component.Owner, SignalTimerUiKey.Key, out var ui))
                return;

            _ui.SetUiState(ui, new SignalTimerBoundUserInterfaceState(component.Text, TimeSpan.FromSeconds(component.Delay).Minutes.ToString("D2"), TimeSpan.FromSeconds(component.Delay).Seconds.ToString("D2")), args.Session);
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
            if (!TryComp(uid, out ActorComponent? actor))
                return;


            if (!_ui.TrySetUiState(component.Owner, SignalTimerUiKey.Key, new SignalTimerBoundUserInterfaceState(component.Text, TimeSpan.FromSeconds(component.Delay).Minutes.ToString("D2"), TimeSpan.FromSeconds(component.Delay).Seconds.ToString("D2"))))
                Logger.DebugS("UIST", "Set UI state failed.");


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

        private void OnTextChangedMessage(EntityUid uid, SignalTimerComponent component, SignalTimerTextChangedMessage args)
        {
            component.Text = args.Text[..Math.Min(5,args.Text.Length)];
            _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, component.Text);
        }

        private void OnDelayChangedMessage(EntityUid uid, SignalTimerComponent component, SignalTimerDelayChangedMessage args)
        {
            component.Delay = args.Delay.TotalSeconds;
        }

        private void OnTimerStartMessage(EntityUid uid, SignalTimerComponent component, SignalTimerStartMessage args)
        {
            component.TriggerTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.Delay);
            component.User = args.User;
            component.Activated = true;

            _appearanceSystem.SetData(uid, TextScreenVisuals.Mode, TextScreenMode.Timer);
            _appearanceSystem.SetData(uid, TextScreenVisuals.TargetTime, component.TriggerTime);
            _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, component.Text);

            _signalSystem.InvokePort(uid, component.StartPort);
        }
    }
}
