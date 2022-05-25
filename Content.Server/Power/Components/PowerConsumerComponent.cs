using Content.Server.Power.NodeGroups;
using Content.Server.Power.Pow3r;

namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Draws power directly from an MV or HV wire it is on top of.
    /// </summary>
    [RegisterComponent]
    public sealed class PowerConsumerComponent : BaseNetConnectorComponent<IBasePowerNet>
    {
        /// <summary>
        ///     How much power this needs to be fully powered.
        /// </summary>
        [DataField("drawRate")]
        [ViewVariables(VVAccess.ReadWrite)]
        public double DrawRate { get => NetworkLoad.DesiredPower; set => NetworkLoad.DesiredPower = value; }

        /// <summary>
        ///     How much power this is currently receiving from <see cref="PowerSupplierComponent"/>s.
        /// </summary>
        [ViewVariables]
        public double ReceivedPower => NetworkLoad.ReceivingPower;

        public double LastReceived = double.NaN;

        public PowerState.Load NetworkLoad { get; } = new();

        protected override void AddSelfToNet(IBasePowerNet powerNet)
        {
            powerNet.AddConsumer(this);
        }

        protected override void RemoveSelfFromNet(IBasePowerNet powerNet)
        {
            powerNet.RemoveConsumer(this);
        }
    }
}
