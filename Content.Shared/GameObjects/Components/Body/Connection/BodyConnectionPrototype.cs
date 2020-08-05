using Content.Shared.GameObjects.Components.Body.Valve;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Connection
{
    public class BodyConnectionPrototype : IExposeData
    {
        public string First { get; private set; }

        public BodyValvePrototype Valve { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, b => b.First, "id", null);
            serializer.DataField(this, b => b.Valve, "valve", null);
        }
    }
}
