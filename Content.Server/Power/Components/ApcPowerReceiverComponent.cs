using System;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.NodeGroups;
using Content.Server.Power.Pow3r;
using Content.Shared.Examine;
using Content.Shared.Power;
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
#pragma warning disable 618
    public class ApcPowerReceiverComponent : Component, IExamine
#pragma warning restore 618
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables]
        public bool Powered => (MathHelper.CloseToPercent(NetworkLoad.ReceivingPower, Load) || !NeedsPower) && !PowerDisabled;

        /// <summary>
        ///     Amount of charge this needs from an APC per second to function.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("powerLoad")]
        public float Load { get => NetworkLoad.DesiredPower; set => NetworkLoad.DesiredPower = value; }

        public ApcPowerProviderComponent? Provider = null;

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

        public float PowerReceived => NetworkLoad.ReceivingPower;

        protected override void OnRemove()
        {
            Provider?.RemoveReceiver(this);

            base.OnRemove();
        }

        public void ApcPowerChanged()
        {
#pragma warning disable 618
            SendMessage(new PowerChangedMessage(Powered));
#pragma warning restore 618
            _entMan.EventBus.RaiseLocalEvent(Owner, new PowerChangedEvent(Powered, NetworkLoad.ReceivingPower));

            if (_entMan.TryGetComponent<AppearanceComponent?>(Owner, out var appearance))
            {
                appearance.SetData(PowerDeviceVisuals.Powered, Powered);
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

#pragma warning disable 618
    public class PowerChangedMessage : ComponentMessage
#pragma warning restore 618
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
        public readonly float ReceivingPower;

        public PowerChangedEvent(bool powered, float receivingPower)
        {
            Powered = powered;
            ReceivingPower = receivingPower;
        }
    }
}
