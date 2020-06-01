using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Diagnostics;

namespace Content.Server.GameObjects.Components.NewPower
{
    [RegisterComponent]
    public class PowerConsumerComponent : BasePowerComponent
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
            serializer.DataField(ref _drawRate, "drawRate", 50);
            serializer.DataField(ref _priority, "priorty", Priority.First);
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
            PowerNet.UpdateConsumerDraw(this, DrawRate, newDrawRate);
            _drawRate = newDrawRate;
        }

        private void SetReceivedPower(int newReceivedPower)
        {
            Debug.Assert(newReceivedPower >= 0 && newReceivedPower <= DrawRate);
            _receivedPower = newReceivedPower;
        }

        private void SetPriority(Priority newPriority)
        {
            PowerNet.UpdateConsumerPriority(this, Priority, newPriority);
            _priority = newPriority;
        }
    }

    public enum Priority
    {
        First,
        Last,
    }
}
