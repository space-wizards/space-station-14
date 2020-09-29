using System.Diagnostics;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.PowerNetComponents
{
    [RegisterComponent]
    public class PowerConsumerComponent : BasePowerNetComponent
    {
        public override string Name => "PowerConsumer";

        /// <summary>
        ///     How much power this needs to be fully powered.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int DrawRate { get => _drawRate; set => SetDrawRate(value); }
        private int _drawRate;

        /// <summary>
        ///     Determines which <see cref="PowerConsumerComponent"/>s receive power when there is not enough
        ///     power for each.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Priority Priority { get => _priority; set => SetPriority(value); }
        private Priority _priority;

        /// <summary>
        ///     How much power this is currently receiving from <see cref="PowerSupplierComponent"/>s.
        /// </summary>
        [ViewVariables]
        public int ReceivedPower { get => _receivedPower; set => SetReceivedPower(value); }
        private int _receivedPower;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _drawRate, "drawRate", 0);
            serializer.DataField(ref _priority, "priority", Priority.First);
        }

        protected override void AddSelfToNet(IPowerNet powerNet)
        {
            powerNet.AddConsumer(this);
        }

        protected override void RemoveSelfFromNet(IPowerNet powerNet)
        {
            powerNet.RemoveConsumer(this);
        }

        private void SetDrawRate(int newDrawRate)
        {
            var oldDrawRate = DrawRate;
            _drawRate = newDrawRate; //must be set before updating powernet, as it checks the DrawRate of every consumer
            Net.UpdateConsumerDraw(this, oldDrawRate, newDrawRate);
        }

        private void SetReceivedPower(int newReceivedPower)
        {
            Debug.Assert(newReceivedPower >= 0 && newReceivedPower <= DrawRate);
            _receivedPower = newReceivedPower;
        }

        private void SetPriority(Priority newPriority)
        {
            Net.UpdateConsumerPriority(this, Priority, newPriority);
            _priority = newPriority;
        }
    }

    public enum Priority
    {
        First,
        Last,
    }
}
