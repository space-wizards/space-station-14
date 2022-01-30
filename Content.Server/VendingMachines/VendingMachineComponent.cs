using System;
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
using Content.Server.Advertise;
using static Content.Shared.Wires.SharedWiresComponent;
using static Content.Shared.Wires.SharedWiresComponent.WiresAction;

namespace Content.Server.VendingMachines
{
    [RegisterComponent]
    public class VendingMachineComponent : SharedVendingMachineComponent, IWires, IBreakAct
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        public bool Ejecting;
        public TimeSpan AnimationDuration = TimeSpan.Zero;
        [DataField("pack")]
        public string PackPrototypeId = string.Empty;
        public string SpriteName = "";
        public bool Broken;
        [DataField("allAccess")]
        public bool AllAccess = false;
        [DataField("speedLimiter")]
        public bool SpeedLimiter = true;
        [DataField("soundVend")]
        // Grabbed from: https://github.com/discordia-space/CEV-Eris/blob/f702afa271136d093ddeb415423240a2ceb212f0/sound/machines/vending_drop.ogg
        public SoundSpecifier SoundVend = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg");
        [DataField("soundDeny")]
        // Yoinked from: https://github.com/discordia-space/CEV-Eris/blob/35bbad6764b14e15c03a816e3e89aa1751660ba9/sound/machines/Custom_deny.ogg
        public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(VendingMachineUiKey.Key);
        public CancellationTokenSource PowerPulsedCancel = new();
        public int PowerPulsedTimeout = 10;
        public float NonLimitedEjectForce = 7.5f;
        public float NonLimitedEjectRange = 5f;

        public void OnBreak(BreakageEventArgs eventArgs)
        {
            Broken = true;
            EntitySystem.Get<VendingMachineSystem>().TryUpdateVisualState(this.Owner, VendingMachineVisualState.Broken, this);
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
            if (!_entMan.TryGetComponent<WiresComponent>(Owner, out var wires)) return;

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

            bool hasAdvert = EntitySystem.Get<VendingMachineSystem>().GetAdvertisementState(this.Owner, this);

            StatusLightState adState = hasAdvert ? StatusLightState.On : StatusLightState.Off;

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

            wires.SetStatus(VendingMachineWireStatus.Power, powerLight);
            wires.SetStatus(VendingMachineWireStatus.Access, accessLight);
            wires.SetStatus(VendingMachineWireStatus.Advertisement, advertisementLight);
            wires.SetStatus(VendingMachineWireStatus.Limiter, limiterLight);
        }

        private bool _powerCut;
        public bool PowerCut
        {
            get => _powerCut;
            set
            {
                _powerCut = value;
            }
        }

        private bool _powerPulsed;
        public bool PowerPulsed
        {
            get => _powerPulsed && !_powerCut;
            set
            {
                _powerPulsed = value;
            }
        }

        public void WiresUpdate(WiresUpdateEventArgs args)
        {
            switch (args.Action)
            {
                case Pulse:
                    switch (args.Identifier)
                    {
                        case Wires.Power:
                            PowerPulsed = true;
                            EntitySystem.Get<VendingMachineSystem>().TryUpdateVisualState(this.Owner, null, this);
                            PowerPulsedCancel.Cancel();
                            PowerPulsedCancel = new CancellationTokenSource();
                            Owner.SpawnTimer(TimeSpan.FromSeconds(PowerPulsedTimeout),
                                () => {
                                    PowerPulsed = false;
                                    UpdateWires();
                                    EntitySystem.Get<VendingMachineSystem>().TryUpdateVisualState(this.Owner, null, this);
                                },
                                PowerPulsedCancel.Token);
                            break;
                        case Wires.Access:
                            if (!PowerPulsed && !PowerCut) {
                                AllAccess = !AllAccess;
                            }
                            break;

                        case Wires.Advertisement:
                            if (!PowerPulsed && !PowerCut) {
                                EntitySystem.Get<AdvertiseSystem>().SayAdvertisement(this.Owner);
                            }
                            break;
                        case Wires.Limiter:
                            if (!PowerPulsed && !PowerCut) {
                                EntitySystem.Get<VendingMachineSystem>().EjectRandom(this.Owner, true, this);
                            }
                            break;
                    }
                    break;
                case Mend:
                    switch (args.Identifier)
                    {
                        case Wires.Power:
                            PowerPulsedCancel.Cancel();
                            PowerPulsed = false;
                            PowerCut = false;
                            EntitySystem.Get<VendingMachineSystem>().TryUpdateVisualState(this.Owner, null, this);
                            break;
                        case Wires.Advertisement:
                            EntitySystem.Get<AdvertiseSystem>().SetEnabled(this.Owner, true);
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
                            EntitySystem.Get<VendingMachineSystem>().TryUpdateVisualState(this.Owner, null, this);
                            break;
                        case Wires.Advertisement:
                            EntitySystem.Get<AdvertiseSystem>().SetEnabled(this.Owner, false);
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

