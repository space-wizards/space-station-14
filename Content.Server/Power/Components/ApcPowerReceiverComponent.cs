using Content.Server.Power.NodeGroups;
using Content.Server.Power.Pow3r;

namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Attempts to link with a nearby <see cref="ApcPowerProviderComponent"/>s
    ///     so that it can receive power from a <see cref="IApcNet"/>.
    /// </summary>
    [RegisterComponent]
    public sealed partial class ApcPowerReceiverComponent : Component
    {
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
                PoweredLastUpdate = null;
            }
        }

        [DataField("needsPower")]
        private bool _needsPower = true;

        /// <summary>
        ///     When true, causes this to never appear powered.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("powerDisabled")]
        public bool PowerDisabled {
            get => !NetworkLoad.Enabled;
            set => NetworkLoad.Enabled = !value;
        }

        public bool? PoweredLastUpdate;

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
    }

    /// <summary>
    /// Raised whenever an ApcPowerReceiver becomes powered / unpowered.
    /// Does nothing on the client.
    /// </summary>
    [ByRefEvent]
    public readonly record struct PowerChangedEvent(bool Powered, float ReceivingPower)
    {
        public readonly bool Powered = Powered;
        public readonly float ReceivingPower = ReceivingPower;
    }

}
