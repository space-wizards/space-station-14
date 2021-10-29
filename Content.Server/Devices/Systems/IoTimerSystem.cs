using System;
using System.Collections.Generic;
using Content.Server.Popups;
using Content.Shared.Devices;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Timing;

namespace Content.Server.Devices.Systems
{
    public class IoTimerSystem : EntitySystem
    {
        [Dependency]
        private readonly UserInterfaceSystem _userInterfaceSystem = default!;

        [Dependency]
        private readonly IGameTiming _gameTiming = default!;

        private readonly List<SharedIoTimerComponent> ActiveTimers = new();

        public override void Initialize()
        {
            SubscribeLocalEvent<SharedIoTimerComponent, UseInHandEvent>(OnUse);
            SubscribeLocalEvent<SharedIoTimerComponent, GetOtherVerbsEvent>(AddConfigureVerb);
            SubscribeLocalEvent<SharedIoTimerComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<SharedIoTimerComponent, ComponentShutdown>(OnShutdown);

            // Bound UI subscriptions
            SubscribeLocalEvent<SharedIoTimerComponent, IoTimerSendToggleMessage>(OnToggleRequested);
            SubscribeLocalEvent<SharedIoTimerComponent, IoTimerUpdateDurationMessage>(UpdateDuration);
            SubscribeLocalEvent<SharedIoTimerComponent, IoTimerSendResetMessage>(OnStopRequested);
            SubscribeLocalEvent<SharedIoTimerComponent, IoTimerSendTogglePauseMessage>(OnPauseRequested);
        }

        private void OnPauseRequested(EntityUid uid, SharedIoTimerComponent component, IoTimerSendTogglePauseMessage args)
        {
            component.IsPaused = !component.IsPaused;

            var curtime = _gameTiming.CurTime;
            //if we're no longer paused, we get the difference between when the timer was last paused
            //and the current time, and add that to the start and end duration of the timer.
            if (!component.IsPaused)
            {
                var oldTime = component.PausedTime;
                var dif = (curtime - oldTime);
                component.StartAndEndTimes.Item1 += dif;
                component.StartAndEndTimes.Item2 += dif;
            }

            component.PausedTime = curtime;
            SyncUi(uid, component);
        }

        private void OnStopRequested(EntityUid uid, SharedIoTimerComponent component, IoTimerSendResetMessage args)
        {
            component.IsPaused = false;
            SetActive(uid, false, component);
        }

        private void OnStartup(EntityUid uid, SharedIoTimerComponent component, ComponentStartup args)
        {
            SetActive(uid, false, component);
            SyncUi(uid, component);
        }

        private void OnShutdown(EntityUid uid, SharedIoTimerComponent component, ComponentShutdown args)
        {
            //make sure our deleted timers are no longer stored in active timers.
            ActiveTimers.Remove(component);
        }

        private void UpdateDuration(EntityUid uid, SharedIoTimerComponent component, IoTimerUpdateDurationMessage args)
        {
            //can't update the duration if the component is active.
            if (component.IsActive)
                return;

            var dur =
                Math.Clamp(args.Duration, SharedIoTimerComponent.MinDuration, SharedIoTimerComponent.MaxDuration);

            component.Duration = dur;
            SyncUi(uid, component);

            var owner = EntityManager.GetEntity(uid);
            if (owner.TryGetContainer(out var container))
            {
                var viewer = container.Owner;
                viewer.PopupMessage(viewer, Loc.GetString("io-timer-component-update-duration"));
            }
        }

        private void OnToggleRequested(EntityUid uid, SharedIoTimerComponent component, IoTimerSendToggleMessage args)
        {
            ToggleTimer(uid, component);
        }


        private void AddConfigureVerb(EntityUid uid, SharedIoTimerComponent component, GetOtherVerbsEvent args)
        {
            if (!args.CanAccess)
                return;

            Verb verb = new();
            verb.Act = () =>
            {
                if (!EntityManager.TryGetComponent<ActorComponent>(args.User.Uid, out var actorComponent))
                    return;
                _userInterfaceSystem.TryOpen(uid, IoTimerUiKey.Key, actorComponent.PlayerSession);
            };
            verb.Text = Loc.GetString("io-timer-component-configure");
            args.Verbs.Add(verb);
        }

        public override void Update(float frameTime)
        {
            var curTime = _gameTiming.CurTime;
            for (var i = ActiveTimers.Count - 1; i >= 0; i--)
            {
                var comp = ActiveTimers[i];
                if (comp.IsPaused)
                    continue;

                if (curTime >= comp.StartAndEndTimes.Item2)
                {
                    var id = comp.Owner.Uid;
                    TriggerTimer(id, comp);
                    SetActive(id, false, comp);
                }
            }
        }

        private void TriggerTimer(EntityUid uid, SharedIoTimerComponent? component)
        {
            if (!Resolve(uid, ref component))
                return;

            var owner = EntityManager.GetEntity(uid);
            if (owner.TryGetContainer(out var container))
            {
                var viewer = container.Owner;
                viewer.PopupMessage(viewer, Loc.GetString("io-timer-component-holder-notify"));

                //if the device is in a container, try to apply an output signal to that container.
                RaiseLocalEvent(container.Owner.Uid, new IoDeviceOutputEvent());
            }
            else
            {
                owner.PopupMessageEveryone(Loc.GetString("io-timer-component-beep"), null, 15);
            }
        }

        private void OnUse(EntityUid uid, SharedIoTimerComponent component, UseInHandEvent args)
        {
            var active = !component.IsActive;
            ToggleTimer(uid, component);

            var owner = EntityManager.GetEntity(uid);
            if (owner.TryGetContainer(out var container))
            {
                var viewer = container.Owner;
                viewer.PopupMessage(viewer, active ? Loc.GetString("io-timer-component-turn-on")
                    : Loc.GetString("io-timer-component-turn-off"));
            }
        }

        public void ToggleTimer(EntityUid uid, SharedIoTimerComponent? component)
        {
            if (!Resolve(uid, ref component))
                return;

            SetActive(uid, !component.IsActive, component);
        }

        public void SyncUi(EntityUid uid, SharedIoTimerComponent? component)
        {
            if (!Resolve(uid, ref component))
                return;

            _userInterfaceSystem.TrySetUiState(uid, IoTimerUiKey.Key,
                new IoTimerBoundUserInterfaceState(component.Duration, component.StartAndEndTimes,
                    component.IsActive, component.IsPaused));
        }

        public void SetActive(EntityUid uid, bool active, SharedIoTimerComponent? component)
        {
            if (!Resolve(uid, ref component))
                return;

            component.IsActive = active;

            if (active)
            {
                if (!ActiveTimers.Contains(component))
                    ActiveTimers.Add(component);

                component.StartAndEndTimes =
                    (_gameTiming.CurTime, _gameTiming.CurTime + TimeSpan.FromSeconds(component.Duration));

                component.IsPaused = false;

                SyncUi(uid, component);
            }
            else
            {
                if (ActiveTimers.Remove(component))
                {
                    //only sync the UI if we actually changed anything.
                    SyncUi(uid, component);
                }
            }
        }
    }
}
