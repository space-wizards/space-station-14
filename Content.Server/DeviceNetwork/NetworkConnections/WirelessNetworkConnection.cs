using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;

namespace Content.Server.GameObjects.EntitySystems.DeviceNetwork
{
    public class WirelessNetworkConnection : BaseNetworkConnection
    {
        public const string WIRELESS_POSITION = "position";

        private readonly IEntity _owner;

        private float _range;
        public float Range { get => _range; set => _range = Math.Abs(value); }

        public WirelessNetworkConnection(int frequency, OnReceiveNetMessage onReceive, bool receiveAll, IEntity owner, float range) : base(NetworkUtils.WIRELESS, frequency, onReceive, receiveAll)
        {
            _owner = owner;
            Range = range;
        }

        protected override bool CanReceive(int frequency, string sender, IReadOnlyDictionary<string, string> payload, Metadata metadata, bool broadcast)
        {
            if (_owner.Deleted)
            {
                Connection.Close();
                return false;
            }

            if (metadata.TryParseMetadata<Vector2>(WIRELESS_POSITION, out var position))
            {
                var ownPosition = _owner.Transform.WorldPosition;
                var distance = (ownPosition - position).Length;
                return distance <= Range;
            }
            //Only receive packages with the same frequency
            return frequency == Frequency;
        }

        protected override Metadata GetMetadata()
        {
            if (_owner.Deleted)
            {
                Connection.Close();
                return new Metadata();
            }

            var position = _owner.Transform.WorldPosition;
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
