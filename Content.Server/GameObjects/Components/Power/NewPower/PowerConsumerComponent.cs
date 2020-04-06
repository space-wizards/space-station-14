using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Diagnostics;

namespace Content.Server.GameObjects.Components.NewPower
{
    /// <summary>
    ///     Requests power from a powernet.
    /// </summary>
    [RegisterComponent]
    public class PowerConsumerComponent : BasePowerNetConnector
    {
        public override string Name => "PowerConsumer";

        /// <summary>
        ///     The amount of electrical power (Watts) wanted by this power consumer.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int DrawRate { get => _drawRate; set => SetDrawRate(value); }
        private int _drawRate;

        /// <summary>
        ///     How much electrical power (Watts) this power consumer is receiving.
        /// </summary>
        [ViewVariables]
        public int ReceivedPower { get => _receivedPower; set => SetReceivedPower(value); }
        private int _receivedPower;

        /// <summary>
        ///     What fraction of the requested power this is receiving.
        /// </summary>
        [ViewVariables]
        public float ReceivedPowerFraction => (float) ReceivedPower / DrawRate;

        /// <summary>
        ///     Priority determines how power is allocated when there is not enough for every power consumer.
        ///     Used by <see cref="PowerNet"/>.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Priority Priority { get => _priority; set => SetPriority(value); }
        private Priority _priority;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _drawRate, "drawRate", 50);
        }

        protected override bool TryJoinPowerNet(PowerNet powerNet)
        {
            return powerNet.TryAddConsumer(this);
        }
        protected override void NotifyPowerNetOfLeaving()
        {
            PowerNet.RemoveConsumer(this);
        }

        private void SetDrawRate(int newDraw)
        {
            var oldDraw = _drawRate;
            _drawRate = newDraw;
            PowerNet?.UpdateConsumerDraw(this, oldDraw, newDraw);
        }

        private void SetReceivedPower(int newPower)
        {
            Debug.Assert( 0 <= newPower && newPower <= DrawRate);
            _receivedPower = newPower;
        }

        private void SetPriority(Priority newPriority)
        {
            var oldPriority = Priority;
            _priority = newPriority;
            PowerNet?.UpdateConsumerPriority(this, oldPriority, newPriority);
        }
    }
}
