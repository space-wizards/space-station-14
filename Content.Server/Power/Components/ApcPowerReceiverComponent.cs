using System;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.NodeGroups;
using Content.Server.Power.Pow3r;
using Content.Shared.Examine;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Attempts to link with a nearby <see cref="ApcPowerProviderComponent"/>s
    ///     so that it can receive power from a <see cref="IApcNet"/>.
    /// </summary>
    [RegisterComponent]
    public class ApcPowerReceiverComponent : Component, IExamine
    {
        [ViewVariables] [ComponentDependency] private readonly IPhysBody? _physicsComponent = null;

        public override string Name => "ApcPowerReceiver";

        [ViewVariables]
        public bool Powered => (MathHelper.CloseToPercent(NetworkLoad.ReceivingPower, Load) || !NeedsPower) && !PowerDisabled;

        /// <summary>
        ///     The max distance from a <see cref="ApcPowerProviderComponent"/> that this can receive power from.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int PowerReceptionRange { get => _powerReceptionRange; set => SetPowerReceptionRange(value); }
        [DataField("powerReceptionRange")]
        private int _powerReceptionRange = 3;

        [ViewVariables]
        public ApcPowerProviderComponent? Provider
        {
            get => _provider;
            set
            {
                // Will get updated before power networks process.
                NetworkLoad.LinkedNetwork = default;
                _provider?.RemoveReceiver(this);
                _provider = value;
                value?.AddReceiver(this);
                ApcPowerChanged();
            }
        }

        private ApcPowerProviderComponent? _provider;

        /// <summary>
        ///     If this should be considered for connection by <see cref="ApcPowerProviderComponent"/>s.
        /// </summary>
        public bool Connectable => Anchored;

        private bool Anchored => _physicsComponent == null || _physicsComponent.BodyType == BodyType.Static;

        [ViewVariables] public bool NeedsProvider => Provider == null;

        /// <summary>
        ///     Amount of charge this needs from an APC per second to function.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("powerLoad")]
        public float Load { get => NetworkLoad.DesiredPower; set => NetworkLoad.DesiredPower = value; }

        /// <summary>
        ///     When false, causes this to appear powered even if not receiving power from an Apc.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool NeedsPower
        {
            get => _needsPower;
            set
            {
                _needsPower = value;
                // Reset this so next tick will do a power update.
                LastPowerReceived = float.NaN;
            }
        }

        [DataField("needsPower")]
        private bool _needsPower = true;

        /// <summary>
        ///     When true, causes this to never appear powered.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("powerDisabled")]
        public bool PowerDisabled { get => !NetworkLoad.Enabled; set => NetworkLoad.Enabled = !value; }

        public float LastPowerReceived = float.NaN;

        [ViewVariables]
        public PowerState.Load NetworkLoad { get; } = new PowerState.Load
        {
            DesiredPower = 5
        };

        protected override void Startup()
        {
            base.Startup();
            if (NeedsProvider)
            {
                TryFindAndSetProvider();
            }
            if (_physicsComponent != null)
            {
                AnchorUpdate();
            }
        }

        protected override void OnRemove()
        {
            _provider?.RemoveReceiver(this);

            base.OnRemove();
        }

        public void TryFindAndSetProvider()
        {
            if (TryFindAvailableProvider(out var provider))
            {
                Provider = provider;
            }
        }

        public void ApcPowerChanged()
        {
            OnNewPowerState();
        }

        private bool TryFindAvailableProvider([NotNullWhen(true)] out ApcPowerProviderComponent? foundProvider)
        {
            var nearbyEntities = IoCManager.Resolve<IEntityLookup>()
                .GetEntitiesInRange(Owner, PowerReceptionRange);

            foreach (var entity in nearbyEntities)
            {
                if (entity.TryGetComponent<ApcPowerProviderComponent>(out var provider))
                {
                    if (provider.Connectable)
                    {
                        if (provider.Owner.Transform.Coordinates.TryDistance(Owner.EntityManager, Owner.Transform.Coordinates, out var distance))
                        {
                            if (distance < Math.Min(PowerReceptionRange, provider.PowerTransferRange))
                            {
                                foundProvider = provider;
                                return true;
                            }
                        }
                    }
                }
            }

            foundProvider = default;
            return false;
        }

        private void SetPowerReceptionRange(int newPowerReceptionRange)
        {
            Provider = null;
            _powerReceptionRange = newPowerReceptionRange;
            TryFindAndSetProvider();
        }

        private void OnNewPowerState()
        {
            SendMessage(new PowerChangedMessage(Powered));
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new PowerChangedEvent(Powered));

            if (Owner.TryGetComponent<AppearanceComponent>(out var appearance))
            {
                appearance.SetData(PowerDeviceVisuals.Powered, Powered);
            }
        }

        public void AnchorUpdate()
        {
            if (Anchored)
            {
                if (NeedsProvider)
                {
                    TryFindAndSetProvider();
                }
            }
            else
            {
                Provider = null;
            }
        }

        ///<summary>
        ///Adds some markup to the examine text of whatever object is using this component to tell you if it's powered or not, even if it doesn't have an icon state to do this for you.
        ///</summary>
        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("power-receiver-component-on-examine-main",
                                            ("stateText", Loc.GetString( Powered ? "power-receiver-component-on-examine-powered" :
                                                                                   "power-receiver-component-on-examine-unpowered"))));
        }
    }

    public class PowerChangedMessage : ComponentMessage
    {
        public readonly bool Powered;

        public PowerChangedMessage(bool powered)
        {
            Powered = powered;
        }
    }

    /// <summary>
    /// Raised whenever an ApcPowerReceiver becomes powered / unpowered.
    /// </summary>
    public sealed class PowerChangedEvent : EntityEventArgs
    {
        public readonly bool Powered;

        public PowerChangedEvent(bool powered)
        {
            Powered = powered;
        }
    }
}
