using System;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Server.WireHacking;
using Content.Shared.Acts;
using Content.Shared.Sound;
using Content.Shared.VendingMachines;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System.Threading;
using Robust.Shared.Maths;
using Content.Server.VendingMachines.systems;

using static Content.Shared.Wires.SharedWiresComponent;
using static Content.Shared.Wires.SharedWiresComponent.WiresAction;
namespace Content.Server.VendingMachines
{
    [RegisterComponent]
    public class VendingMachineComponent : SharedVendingMachineComponent, IWires, IBreakAct
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [ComponentDependency] private readonly WiresComponent? WiresComponent = null;

        private VendingMachineSystem? _vendingMachineSystem;
        public bool Ejecting;
        public TimeSpan _animationDuration = TimeSpan.Zero;
        [DataField("pack")]
        public string PackPrototypeId = string.Empty;
        public string SpriteName = "";
        public bool Powered => (PowerPulsed || PowerCut) ? false : !_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;
        public bool Broken;
        [DataField("allAccess")]
        public bool AllAccess = false;
        [DataField("speedLimiter")]
        public bool SpeedLimiter = true;
        [DataField("soundVend")]
        // Grabbed from: https://github.com/discordia-space/CEV-Eris/blob/f702afa271136d093ddeb415423240a2ceb212f0/sound/machines/vending_drop.ogg
        public SoundSpecifier soundVend = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg");
        [DataField("soundDeny")]
        // Yoinked from: https://github.com/discordia-space/CEV-Eris/blob/35bbad6764b14e15c03a816e3e89aa1751660ba9/sound/machines/Custom_deny.ogg
        public SoundSpecifier _soundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(VendingMachineUiKey.Key);
        private CancellationTokenSource _powerPulsedCancel = new();
        private int PowerPulsedTimeout = 10;
        public float NonLimitedEjectForce = 7.5f;
        public float NonLimitedEjectRange = 5f;

        public void OnUiReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            if (!Powered)
                return;

            if (_vendingMachineSystem == null)
                _vendingMachineSystem = EntitySystem.Get<VendingMachineSystem>();

            var message = serverMsg.Message;
            switch (message)
            {
                case VendingMachineEjectMessage msg:
                    _vendingMachineSystem.AuthorizedVend(this, msg.ID, serverMsg.Session.AttachedEntity);
                    break;
                case InventorySyncRequestMessage _:
                    UserInterface?.SendMessage(new VendingMachineInventoryMessage(Inventory));
                    break;
            }
        }
        public void OnBreak(BreakageEventArgs eventArgs)
        {

            if (_vendingMachineSystem == null)
                _vendingMachineSystem = EntitySystem.Get<VendingMachineSystem>();

            Broken = true;
            _vendingMachineSystem.TryUpdateVisualState(this, VendingMachineVisualState.Broken);
        }
        private enum Wires
        {
            // Pulsing it disrupts power.
            // Cutting this kills power.
            Power,
            // Pulsing allows anyone to dispense.
            // Cutting does nothing.
            Access,
            // Pulsing shoots a random item out.
            // Cutting will make any dispensed item fire out.
            Limiter,
            // Pulsing causes an ad to play immediately
            // Cutting stops ads from playing
            Advertisement
        }

        public void RegisterWires(WiresComponent.WiresBuilder builder)
        {
            foreach (var wire in Enum.GetValues<Wires>())
                builder.CreateWire(wire);

            UpdateWires();
        }

        public void UpdateWires()
        {
            if (_vendingMachineSystem == null)
                _vendingMachineSystem = EntitySystem.Get<VendingMachineSystem>();

            if (WiresComponent == null) return;

            var pwrLightState = (PowerPulsed, PowerCut) switch {
                (true, false) => StatusLightState.BlinkingFast,
                (_, true) => StatusLightState.Off,
                (_, _) => StatusLightState.On
            };

            var powerLight = new StatusLightData(Color.Yellow, pwrLightState, "POWER");

            var accessLight = new StatusLightData(
                Color.Red,
                AllAccess ? StatusLightState.Off : StatusLightState.On,
                "ACCESS"
            );

            bool? hasAdvert = _vendingMachineSystem.GetAdvertisementState(this);

            StatusLightState adState = hasAdvert != null ?
            (hasAdvert == true ? StatusLightState.On : StatusLightState.BlinkingSlow)
            : StatusLightState.Off;

            var advertisementLight = new StatusLightData(
                Color.Green,
                adState,
                "ADVERT"
            );

            var limiterLight = new StatusLightData(
                Color.DarkSalmon,
                (SpeedLimiter ? StatusLightState.On : StatusLightState.Off),
                "LIMITER"
            );

            WiresComponent.SetStatus(VendingMachineWireStatus.Power, powerLight);
            WiresComponent.SetStatus(VendingMachineWireStatus.Access, accessLight);
            WiresComponent.SetStatus(VendingMachineWireStatus.Advertisement, advertisementLight);
            WiresComponent.SetStatus(VendingMachineWireStatus.Limiter, limiterLight);
        }

        private bool _powerCut;
        private bool PowerCut
        {
            get => _powerCut;
            set
            {
                _powerCut = value;
            }
        }

        private bool _powerPulsed;
        private bool PowerPulsed
        {
            get => _powerPulsed && !_powerCut;
            set
            {
                _powerPulsed = value;
            }
        }

        public void WiresUpdate(WiresUpdateEventArgs args)
        {

            if (_vendingMachineSystem == null)
                _vendingMachineSystem = EntitySystem.Get<VendingMachineSystem>();

            switch (args.Action)
            {
                case Pulse:
                    switch (args.Identifier)
                    {
                        case Wires.Power:
                            PowerPulsed = true;
                            _vendingMachineSystem.TryUpdateVisualState(this);
                            _powerPulsedCancel.Cancel();
                            _powerPulsedCancel = new CancellationTokenSource();
                            Owner.SpawnTimer(TimeSpan.FromSeconds(PowerPulsedTimeout),
                                () => {
                                    PowerPulsed = false;
                                    UpdateWires();
                                    _vendingMachineSystem.TryUpdateVisualState(this);
                                },
                                _powerPulsedCancel.Token);
                            break;
                        case Wires.Access:
                            if (!PowerPulsed && !PowerCut) {
                                AllAccess = !AllAccess;
                            }
                            break;

                        case Wires.Advertisement:
                            if (!PowerPulsed && !PowerCut) {
                                _vendingMachineSystem.SayAdvertisement(this);
                            }
                            break;
                        case Wires.Limiter:
                            if (!PowerPulsed && !PowerCut) {
                                _vendingMachineSystem.EjectRandom(this);
                            }
                            break;
                    }
                    break;
                case Mend:
                    switch (args.Identifier)
                    {
                        case Wires.Power:
                            _powerPulsedCancel.Cancel();
                            PowerPulsed = false;
                            PowerCut = false;
                            _vendingMachineSystem.TryUpdateVisualState(this);
                            break;
                        case Wires.Advertisement:
                            _vendingMachineSystem.SetAdvertisementState(this, true);
                        break;
                        case Wires.Limiter:
                            SpeedLimiter = true;
                        break;
                    }
                    break;
                case Cut:
                    switch (args.Identifier)
                    {
                        case Wires.Power:
                            PowerCut = true;
                            _vendingMachineSystem.TryUpdateVisualState(this);
                            break;
                        case Wires.Advertisement:
                            _vendingMachineSystem.SetAdvertisementState(this, false);
                        break;
                        case Wires.Limiter:
                            SpeedLimiter = false;
                        break;
                    }
                    break;
            }
            UpdateWires();
        }
    }

    public class WiresUpdateEventArgs : EventArgs
    {
        public readonly object Identifier;
        public readonly WiresAction Action;

        public WiresUpdateEventArgs(object identifier, WiresAction action)
        {
            Identifier = identifier;
            Action = action;
        }
    }

    public interface IWires
    {
        void RegisterWires(WiresComponent.WiresBuilder builder);
        void WiresUpdate(WiresUpdateEventArgs args);

    }
}

