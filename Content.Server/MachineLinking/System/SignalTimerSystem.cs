using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Server.MachineLinking.Components;
using Content.Shared.TextScreen;
using Robust.Server.GameObjects;
using Content.Shared.MachineLinking;
using Content.Server.UserInterface;
using Content.Shared.Access.Systems;
using Content.Server.Interaction;

namespace Content.Server.MachineLinking.System
{

    public sealed partial class SignalTimerSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly InteractionSystem _interaction = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SignalTimerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SignalTimerComponent, AfterActivatableUIOpenEvent>(OnAfterActivatableUIOpen);

            SubscribeLocalEvent<SignalTimerComponent, SignalTimerTextChangedMessage>(OnTextChangedMessage);
            SubscribeLocalEvent<SignalTimerComponent, SignalTimerDelayChangedMessage>(OnDelayChangedMessage);
            SubscribeLocalEvent<SignalTimerComponent, SignalTimerStartMessage>(OnTimerStartMessage);
        }

        private void OnInit(EntityUid uid, SignalTimerComponent component, ComponentInit args)
        {
            if (TryComp(uid, out SignalTransmitterComponent? comp))
            {
                comp.Outputs.TryAdd(component.TriggerPort, new());
                comp.Outputs.TryAdd(component.StartPort, new());
            }

            _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, component.Text);
        }

        private void OnAfterActivatableUIOpen(EntityUid uid, SignalTimerComponent component, AfterActivatableUIOpenEvent args)
        {
            if (_ui.TryGetUi(component.Owner, SignalTimerUiKey.Key, out var bui))
                _ui.SetUiState(bui, new SignalTimerBoundUserInterfaceState(component.Text, TimeSpan.FromSeconds(component.Delay).Minutes.ToString("D2"), TimeSpan.FromSeconds(component.Delay).Seconds.ToString("D2"), component.CanEditText, component.TriggerTime, component.Activated, _accessReader.IsAllowed(args.User, uid)));
        }

        public bool Trigger(EntityUid uid, SignalTimerComponent signalTimer)
        {
            signalTimer.Activated = false;

            _signalSystem.InvokePort(uid, signalTimer.TriggerPort);

            _appearanceSystem.SetData(uid, TextScreenVisuals.Mode, TextScreenMode.Text);

            if (_ui.TryGetUi(uid, SignalTimerUiKey.Key, out var bui))
                _ui.SetUiState(bui, new SignalTimerBoundUserInterfaceState(signalTimer.Text, TimeSpan.FromSeconds(signalTimer.Delay).Minutes.ToString("D2"), TimeSpan.FromSeconds(signalTimer.Delay).Seconds.ToString("D2"), signalTimer.CanEditText, signalTimer.TriggerTime, signalTimer.Activated, null));

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
                        _audio.Play(timer.DoneSound, filter, timer.Owner, timer.SoundParams);
                    }
                }
            }
        }

        private void OnTextChangedMessage(EntityUid uid, SignalTimerComponent component, SignalTimerTextChangedMessage args)
        {
            if (args.Session.AttachedEntity is not { Valid: true } mob)
                return;

            if (!_interaction.InRangeUnobstructed(mob, uid))
                return;

            if (!_accessReader.IsAllowed(mob, uid))
                return;

            component.Text = args.Text[..Math.Min(5,args.Text.Length)];
            _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, component.Text);
        }

        private void OnDelayChangedMessage(EntityUid uid, SignalTimerComponent component, SignalTimerDelayChangedMessage args)
        {
            if (args.Session.AttachedEntity is not { Valid: true } mob)
                return;

            if (!_interaction.InRangeUnobstructed(mob, uid))
                return;

            if (!_accessReader.IsAllowed(mob,uid))
                return;

            component.Delay = args.Delay.TotalSeconds;
        }

        private void OnTimerStartMessage(EntityUid uid, SignalTimerComponent component, SignalTimerStartMessage args)
        {
            if (args.Session.AttachedEntity is not { Valid: true } mob)
                return;

            if (!_interaction.InRangeUnobstructed(mob, uid))
                return;

            if (!_accessReader.IsAllowed(mob, uid))
                return;

            if (!component.Activated)
            {
                component.TriggerTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.Delay);
                component.User = args.User;
                component.Activated = true;

                _appearanceSystem.SetData(uid, TextScreenVisuals.Mode, TextScreenMode.Timer);
                _appearanceSystem.SetData(uid, TextScreenVisuals.TargetTime, component.TriggerTime);
                _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, component.Text);

                _signalSystem.InvokePort(uid, component.StartPort);
            }
            else
            {
                component.User = args.User;
                component.Activated = false;

                _appearanceSystem.SetData(uid, TextScreenVisuals.Mode, TextScreenMode.Text);
                _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, component.Text);
            }
        }
    }
}
