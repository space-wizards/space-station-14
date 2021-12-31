using System;
using Content.Server.Power.NodeGroups;
using Content.Server.UserInterface;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.APC;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ApcComponent : BaseApcNetComponent, IActivate
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "Apc";

        public bool MainBreakerEnabled { get; private set; } = true;

        [DataField("onReceiveMessageSound")] private SoundSpecifier _onReceiveMessageSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        private ApcChargeState _lastChargeState;

        private TimeSpan _lastChargeStateChange;

        private ApcExternalPowerState _lastExternalPowerState;

        private TimeSpan _lastExternalPowerStateChange;

        private float _lastCharge;

        private TimeSpan _lastChargeChange;

        private bool _uiDirty = true;

        private const float HighPowerThreshold = 0.9f;

        private const int VisualsChangeDelay = 1;

        private static readonly Color LackColor = Color.FromHex("#d1332e");
        private static readonly Color ChargingColor = Color.FromHex("#2e8ad1");
        private static readonly Color FullColor = Color.FromHex("#3db83b");

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(ApcUiKey.Key);

        public BatteryComponent? Battery => _entMan.TryGetComponent(Owner, out BatteryComponent? batteryComponent) ? batteryComponent : null;

        [ComponentDependency] private AccessReaderComponent? _accessReader = null;

        protected override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn<ServerUserInterfaceComponent>();
            Owner.EnsureComponentWarn<AccessReaderComponent>();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }

            Update();
        }

        protected override void AddSelfToNet(IApcNet apcNet)
        {
            apcNet.AddApc(this);
        }

        protected override void RemoveSelfFromNet(IApcNet apcNet)
        {
            apcNet.RemoveApc(this);
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            if (serverMsg.Message is not ApcToggleMainBreakerMessage || serverMsg.Session.AttachedEntity is not {} attached)
                return;

            var accessSystem = EntitySystem.Get<AccessReaderSystem>();
            if (_accessReader == null || accessSystem.IsAllowed(_accessReader, attached))
            {
                MainBreakerEnabled = !MainBreakerEnabled;
                _entMan.GetComponent<PowerNetworkBatteryComponent>(Owner).CanDischarge = MainBreakerEnabled;

                _uiDirty = true;
                SoundSystem.Play(Filter.Pvs(Owner), _onReceiveMessageSound.GetSound(), Owner, AudioParams.Default.WithVolume(-2f));
            }
            else
            {
                attached.PopupMessageCursor(Loc.GetString("apc-component-insufficient-access"));
            }
        }

        public void Update()
        {
            var newState = CalcChargeState();
            if (newState != _lastChargeState && _lastChargeStateChange + TimeSpan.FromSeconds(VisualsChangeDelay) < _gameTiming.CurTime)
            {
                _lastChargeState = newState;
                _lastChargeStateChange = _gameTiming.CurTime;

                if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
                {
                    appearance.SetData(ApcVisuals.ChargeState, newState);
                }

                if (_entMan.TryGetComponent(Owner, out SharedPointLightComponent? light))
                {
                    light.Color = newState switch
                    {
                        ApcChargeState.Lack => LackColor,
                        ApcChargeState.Charging => ChargingColor,
                        ApcChargeState.Full => FullColor,
                        _ => LackColor
                    };
                    light.Dirty();
                }
            }

            _entMan.TryGetComponent(Owner, out BatteryComponent? battery);

            var newCharge = battery?.CurrentCharge;
            if (newCharge != null && newCharge != _lastCharge && _lastChargeChange + TimeSpan.FromSeconds(VisualsChangeDelay) < _gameTiming.CurTime)
            {
                _lastCharge = newCharge.Value;
                _lastChargeChange = _gameTiming.CurTime;
                _uiDirty = true;
            }

            var extPowerState = CalcExtPowerState();
            if (extPowerState != _lastExternalPowerState && _lastExternalPowerStateChange + TimeSpan.FromSeconds(VisualsChangeDelay) < _gameTiming.CurTime)
            {
                _lastExternalPowerState = extPowerState;
                _lastExternalPowerStateChange = _gameTiming.CurTime;
                _uiDirty = true;
            }

            if (_uiDirty && battery != null && newCharge != null)
            {
                UserInterface?.SetState(new ApcBoundInterfaceState(MainBreakerEnabled, extPowerState, newCharge.Value / battery.MaxCharge));
                _uiDirty = false;
            }
        }

        private ApcChargeState CalcChargeState()
        {
            if (!_entMan.TryGetComponent(Owner, out BatteryComponent? battery))
            {
                return ApcChargeState.Lack;
            }

            var chargeFraction = battery.CurrentCharge / battery.MaxCharge;

            if (chargeFraction > HighPowerThreshold)
            {
                return ApcChargeState.Full;
            }

            var netBattery = _entMan.GetComponent<PowerNetworkBatteryComponent>(Owner);
            var delta = netBattery.CurrentSupply - netBattery.CurrentReceiving;

            return delta < 0 ? ApcChargeState.Charging : ApcChargeState.Lack;
        }

        private ApcExternalPowerState CalcExtPowerState()
        {
            var bat = Battery;
            if (bat == null)
                return ApcExternalPowerState.None;

            var netBat = _entMan.GetComponent<PowerNetworkBatteryComponent>(Owner);
            if (netBat.CurrentReceiving == 0 && netBat.LoadingNetworkDemand != 0)
            {
                return ApcExternalPowerState.None;
            }

            var delta = netBat.CurrentReceiving - netBat.LoadingNetworkDemand;
            if (!MathHelper.CloseToPercent(delta, 0, 0.1f) && delta < 0)
            {
                return ApcExternalPowerState.Low;
            }

            return ApcExternalPowerState.Good;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!_entMan.TryGetComponent(eventArgs.User, out ActorComponent? actor))
            {
                return;
            }

            UserInterface?.Open(actor.PlayerSession);
        }
    }
}
