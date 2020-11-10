using Content.Server.DeviceNetwork;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.DeviceNetworkConnections
{
    public class WirelessNetworkConnection : BaseNetworkConnectionComponent
    {
        public const string WIRELESS_POSITION = "position";

        public override string Name => "WirelessNetworkConnection";

        private float _range;
        public float Range { get => _range; set => _range = Math.Abs(value); }

        protected override int DeviceNetID => NetworkUtils.WIRELESS;

        private int _frequency;
        protected override int DeviceNetFrequency => _frequency;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _range, "Range", 100);
            serializer.DataField(ref _frequency, "Frequency", 100);
        }

        protected override bool CanReceive(int frequency, string sender, IReadOnlyDictionary<string, string> payload, Metadata metadata, bool broadcast)
        {
            if (metadata.TryParseMetadata<Vector2>(WIRELESS_POSITION, out var position))
            {
                var ownPosition = Owner.Transform.WorldPosition;
                var distance = (ownPosition - position).Length;
                return distance <= Range;
            }
            //Only receive packages with the same frequency
            return frequency == Frequency;
        }

        protected override Metadata GetMetadata()
        {

            var position = Owner.Transform.WorldPosition;
            var metadata = new Metadata
            {
                {WIRELESS_POSITION, position}
            };

            return metadata;
        }

        protected override Dictionary<string, string> ManipulatePayload(Dictionary<string, string> payload)
        {
            return payload;
        }
    }
}
