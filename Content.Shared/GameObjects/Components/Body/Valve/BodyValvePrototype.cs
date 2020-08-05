using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Valve
{
    public class BodyValvePrototype : IExposeData
    {
        public string First { get; private set; }

        public int MaxPressure { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, b => b.First, "id", null);
            serializer.DataField(this, b => b.MaxPressure, "maxPressure", 100); // TODO
        }
    }
}