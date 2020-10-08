using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;

namespace Content.Server.GameObjects.EntitySystems.DeviceNetwork
{
    public class WirelessNetworkConnection : BaseNetworkConnection
    {

        private readonly IEntity _owner;

        private float _range;
        public float Range { get => _range; set => _range = Math.Abs(value); }

        public WirelessNetworkConnection(int frequency, OnReceiveNetMessage onReceive, bool receiveAll, IEntity owner, float range) : base(2, frequency, onReceive, receiveAll)
        {
            _owner = owner;
            Range = range;
        }

        protected override bool CanReceive(int frequency, string sender, IReadOnlyDictionary<string, string> payload, Metadata metadata, bool broadcast)
        {
            if(_owner.TryGetComponent<ITransformComponent>(out var transform) && metadata.TryParseMetadata<Vector2>("position", out var position))
            {
                var ownPosition = transform.WorldPosition;
                var distance = (ownPosition - position).Length;
                return distance <= Range;
            }
            return true;
        }

        protected override Metadata GetMetadata()
        {
            if(_owner.TryGetComponent<ITransformComponent>(out var transform))
            {
                var position = transform.WorldPosition;
                var metadata = new Metadata
                {
                    {"position", position}
                };

                return metadata;
            }

            return new Metadata();
        }

        protected override Dictionary<string, string> ManipulatePayload(Dictionary<string, string> payload)
        {
            return payload;
        }
    }
}
