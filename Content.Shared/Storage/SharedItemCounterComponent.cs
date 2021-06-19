using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage
{
    [RegisterComponent]
    public class SharedItemCounterComponent : Component, ISerializationHooks
    {
        public override string Name => "ItemCounter";
    }
}
