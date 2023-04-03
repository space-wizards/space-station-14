using Content.Server.Emp;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.APC;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Timing;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ApcSystem : EntitySystem
    {
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(PowerNetSystem));

            SubscribeLocalEvent<ApcComponent, BoundUIOpenedEvent>(OnBoundUiOpen);
            SubscribeLocalEvent<ApcComponent, MapInitEvent>(OnApcInit);
            SubscribeLocalEvent<ApcComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
            SubscribeLocalEvent<ApcComponent, ApcToggleMainBreakerMessage>(OnToggleMainBreaker);
            SubscribeLocalEvent<ApcComponent, GotEmaggedEvent>(OnEmagged);

            SubscribeLocalEvent<ApcComponent, EmpPulseEvent>(OnEmpPulse);
        }

        // Change the APC's state only when the battery state changes, or when it's first created.
        private void OnBatteryChargeChanged(EntityUid uid, ApcComponent component, ChargeChangedEvent args)
        {
            UpdateApcState(uid, component);
        }

        private void OnApcInit(EntityUid uid, ApcComponent component, MapInitEvent args)
        {
            UpdateApcState(uid, component);
        }
        //Update the HasAccess var for UI to read
        private void OnBoundUiOpen(EntityUid uid, ApcComponent component, BoundUIOpenedEvent args)
        {
            TryComp<AccessReaderComponent>(uid, out var access);
            if (args.Session.AttachedEntity == null)
                return;

            if (access == null || _accessReader.IsAllowed(args.Session.AttachedEntity.Value, access))
            {
                component.HasAccess = true;
            }
            else
            {
                component.HasAccess = false;
            }
        }
        private void OnToggleMainBreaker(EntityUid uid, ApcComponent component, ApcToggleMainBreakerMessage args)
        {
            TryComp<AccessReaderComponent>(uid, out var access);
            if (args.Session.AttachedEntity == null)
                return;

            if (access == null || _accessReader.IsAllowed(args.Session.AttachedEntity.Value, access))
            {
                ApcToggleBreaker(uid, component);
            }
            else
            {
                _popup.PopupCursor(Loc.GetString("apc-component-insufficient-access"),
                    args.Session, PopupType.Medium);
            }
        }

        public void ApcToggleBreaker(EntityUid uid, ApcComponent? apc = null, PowerNetworkBatteryComponent? battery = null)
        {
            if (!Resolve(uid, ref apc, ref battery))
                return;

            apc.MainBreakerEnabled = !apc.MainBreakerEnabled;
            battery.CanDischarge = apc.MainBreakerEnabled;

            UpdateUIState(uid, apc);
            _audio.PlayPvs(apc.OnReceiveMessageSound, uid, AudioParams.Default.WithVolume(-2f));
        }

        private void OnEmagged(EntityUid uid, ApcComponent comp, ref GotEmaggedEvent args)
        {
            // no fancy conditions
            args.Handled = true;
        }

        public void UpdateApcState(EntityUid uid,
            ApcComponent? apc=null,
            BatteryComponent? battery=null)
        {
            if (!Resolve(uid, ref apc, ref battery))
                return;

            var newState = CalcChargeState(uid, apc, battery);
            if (newState != apc.LastChargeState && apc.LastChargeStateTime + ApcComponent.VisualsChangeDelay < _gameTiming.CurTime)
            {
                apc.LastChargeState = newState;
                apc.LastChargeStateTime = _gameTiming.CurTime;

                if (TryComp(uid, out AppearanceComponent? appearance))
                {
                    _appearance.SetData(uid, ApcVisuals.ChargeState, newState, appearance);
                }
            }

            var extPowerState = CalcExtPowerState(uid, apc, battery);
            if (extPowerState != apc.LastExternalState
                || apc.LastUiUpdate + ApcComponent.VisualsChangeDelay < _gameTiming.CurTime)
            {
                apc.LastExternalState = extPowerState;
                apc.LastUiUpdate = _gameTiming.CurTime;
                UpdateUIState(uid, apc, battery);
            }
        }

        public void UpdateUIState(EntityUid uid,
            ApcComponent? apc = null,
            BatteryComponent? battery = null,
            ServerUserInterfaceComponent? ui = null)
        {
            if (!Resolve(uid, ref apc, ref battery, ref ui))
                return;

            var netBattery = Comp<PowerNetworkBatteryComponent>(uid);
            float power = netBattery is not null ? netBattery.CurrentSupply : 0f;
            if (_ui.TryGetUi(uid, ApcUiKey.Key, out var bui, ui))
            {
                var state = new ApcBoundInterfaceState(
                    apc.MainBreakerEnabled,
                    apc.HasAccess, (int) MathF.Ceiling(power),
                    apc.LastExternalState,
                    battery.CurrentCharge / battery.MaxCharge
                );
                _ui.SetUiState(bui, state);
            }
        }

        public ApcChargeState CalcChargeState(EntityUid uid,
            ApcComponent? apc=null,
            BatteryComponent? battery=null)
        {
            if (apc != null && HasComp<EmaggedComponent>(uid))
                return ApcChargeState.Emag;

            if (!Resolve(uid, ref apc, ref battery))
                return ApcChargeState.Lack;

            var chargeFraction = battery.CurrentCharge / battery.MaxCharge;

            if (chargeFraction > ApcComponent.HighPowerThreshold)
            {
                return ApcChargeState.Full;
            }

            var netBattery = Comp<PowerNetworkBatteryComponent>(uid);
            var delta = netBattery.CurrentSupply - netBattery.CurrentReceiving;

            return delta < 0 ? ApcChargeState.Charging : ApcChargeState.Lack;
        }

        public ApcExternalPowerState CalcExtPowerState(EntityUid uid,
            ApcComponent? apc=null,
            BatteryComponent? battery=null)
        {
            if (!Resolve(uid, ref apc, ref battery))
                return ApcExternalPowerState.None;

            var netBat = Comp<PowerNetworkBatteryComponent>(uid);
            if (netBat.CurrentReceiving == 0 && !MathHelper.CloseTo(battery.CurrentCharge / battery.MaxCharge, 1))
            {
                return ApcExternalPowerState.None;
            }

            var delta = netBat.CurrentReceiving - netBat.CurrentSupply;
            if (!MathHelper.CloseToPercent(delta, 0, 0.1f) && delta < 0)
            {
                return ApcExternalPowerState.Low;
            }

            return ApcExternalPowerState.Good;
        }

        private void OnEmpPulse(EntityUid uid, ApcComponent component, ref EmpPulseEvent args)
        {
            if (component.MainBreakerEnabled)
            {
                args.Affected = true;
                ApcToggleBreaker(uid, component);
            }
        }
    }
}
