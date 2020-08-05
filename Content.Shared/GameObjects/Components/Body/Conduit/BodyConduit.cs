using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Body.Connection;
using Content.Shared.GameObjects.Components.Body.Connector;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Body.Conduit
{
    public class BodyConduit : IBodyConduit
    {
        private string _conduitId;
        private string _conduitName;

        [ViewVariables]
        public string ConduitId => _conduitId;

        [ViewVariables]
        public string ConduitName => _conduitName;
        
        public BodySubstanceType Type { get; set; }
        
        [ViewVariables]
        public int MaxVolume { get; set; }

        [ViewVariables]
        public double MaxVolumeUsOunces => MaxVolume * 0.033814023;

        [ViewVariables]
        public string Part { get; set; }
        
        [ViewVariables]
        public List<BodyConnection> Connections { get; set; }

        public void Initialize()
        {
            
        }

        public bool TryConnect(IBodyConnector other)
        {
            return true; // TODO
        }

        public IBodyConduit Copy()
        {
            return new BodyConduit
            {
                _conduitId = _conduitId,
                _conduitName = _conduitName,
                Type = Type,
                MaxVolume = MaxVolume,
                Part = Part,
                Connections = Connections
            };
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _conduitId, "id", "");
            serializer.DataField(ref _conduitName, "name", "");
            serializer.DataField(this, b => b.Type, "type", BodySubstanceType.None);
            serializer.DataField(this, b => b.MaxVolume, "volume", 100);
            serializer.DataField(this, b => b.Part, "parts", null);
            serializer.DataField(this, b => b.Connections, "connections", new List<BodyConnection>());
        }
    }
}
