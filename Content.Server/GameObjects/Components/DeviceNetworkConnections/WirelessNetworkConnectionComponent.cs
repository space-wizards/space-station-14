using Content.Server.DeviceNetwork;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.DeviceNetworkConnections
{
    /// <summary>
    /// Sends and receives device network messages wirelessly. Devices sending and receiving need to be in range and on the same frequency.
    /// </summary>
    [RegisterComponent]
    public class WirelessNetworkConnectionComponent : BaseNetworkConnectionComponent
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

        /// <summary>
        /// Checks if the message was sent by a device that is in range and on the same frequency.
        /// </summary>
        protected override bool CanReceive(int frequency, string sender, NetworkPayload payload, Dictionary<string, object> metadata, bool broadcast)
        {
            if (metadata.TryParseMetadata<Vector2>(WIRELESS_POSITION, out var position))
            {
                var ownPosition = Owner.Transform.WorldPosition;
                var distance = (ownPosition - position).Length;
                return distance <= Range && frequency == Frequency;
            }
            //Only receive packages with the same frequency
            return frequency == Frequency;
        }

        protected override Dictionary<string, object> GetMetadata()
        {

            var position = Owner.Transform.WorldPosition;
            var metadata = new Dictionary<string, object>
            {
                {WIRELESS_POSITION, position}
            };

            return metadata;
        }

        protected override NetworkPayload ManipulatePayload(NetworkPayload payload)
        {
            return payload;
        }
    }
}
