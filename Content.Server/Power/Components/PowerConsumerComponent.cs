using Content.Server.Power.NodeGroups;
using Content.Server.Power.Pow3r;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.NodeGroups;

namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Draws power directly from the wire it is on top of.
    /// </summary>
    [RegisterComponent]
    public sealed class PowerConsumerComponent : BaseNetConnectorComponent<IBasePowerNet>
    {
        /// <summary>
        ///     How much power this needs to be fully powered.
        /// </summary>
        [DataField("drawRate")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float DrawRate { get => NetworkLoad.DesiredPower; set => NetworkLoad.DesiredPower = value; }

        /// <summary>
        ///  True if the consumer is receiving power
        /// </summary>
        [ViewVariables]
        public bool Powered { get => DrawRate <= ReceivedPower; set => Powered = value; }

        /// <summary>
        ///     How much power this is currently receiving from <see cref="PowerSupplierComponent"/>s.
        /// </summary>
        [ViewVariables]
        public float ReceivedPower => NetworkLoad.ReceivingPower;

        public float LastReceived = float.NaN;

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
