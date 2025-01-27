using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.CollectiveMind
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class CollectiveMindComponent : Component
    {
        [DataField("minds")]
        public Dictionary<string, int> Minds = new();
    }
}
