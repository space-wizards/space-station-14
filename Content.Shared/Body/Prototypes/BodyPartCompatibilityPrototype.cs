using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Prototypes
{
    /// <summary>
    ///     A body part compatbility type, can be used to limit which parts can be attached to each other and which mechanism can go in which body part
    /// </summary>
    [Prototype("bodyPartCompatibility")]
    [Serializable, NetSerializable]
    public sealed class BodyPartCompatibilityPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; } = default!;
    }
}
