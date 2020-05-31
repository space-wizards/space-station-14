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

        [ViewVariables(VVAccess.ReadWrite)]
        public int DrawRate { get => _drawRate; set => SetDrawRate(value); }
        private int _drawRate;

        [ViewVariables]
        public int ReceivedPower { get => _receivedPower; set => SetReceivedPower(value); }
        private int _receivedPower;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _drawRate, "drawRate", 50);
        }

        protected override void AddSelfToNet(IPowerNet powerNet)
        {
            powerNet.AddConsumer(this);
        }

        protected override void RemoveSelfFromNet(IPowerNet powerNet)
        {
            powerNet.RemoveConsumer(this);
        }

        private void SetDrawRate(int newDraw)
        {
            throw new NotImplementedException();
        }

        private void SetReceivedPower(int newReceivedPower)
        {
            Debug.Assert(newReceivedPower > 0 && newReceivedPower < DrawRate);
            _receivedPower = newReceivedPower;
        }
    }
}
