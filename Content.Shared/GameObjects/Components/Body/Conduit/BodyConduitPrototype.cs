using System.Collections.Generic;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Conduit
{
    public class BodyConduitPrototype : IExposeData
    {
        // TODO: Null?
        public string Id { get; private set; }
        
        public string Name { get; private set; }
        
        public BodySubstanceType Type { get; private set; }
        
        public int MaxVolume { get; private set; }
        
        public List<string> Connections { get; private set; }
        
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, b => b.Id, "id", "");
            serializer.DataField(this, b => b.Name, "name", "");
            serializer.DataField(this, b => b.Type, "type", BodySubstanceType.None);
            serializer.DataField(this, b => b.MaxVolume, "maxVolume", 100);
            serializer.DataField(this, b => b.Part, "part", null);
            serializer.DataField(this, b => b.Connections, "connections", new List<BodyConnection>());
        }
    }
}