using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.CollectiveMind
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class CollectiveMindComponent : Component
    {
        [DataField("minds")]
        public Dictionary<CollectiveMindPrototype, CollectiveMindMemberData> Minds = new();
    }

    /// <summary>
    /// Stores data about the collective mind member.
    /// </summary>
    [Serializable]
    public sealed class CollectiveMindMemberData
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public int MindId = 1; //this value determines the starting mind id for members of the collective mind.
    }
}
