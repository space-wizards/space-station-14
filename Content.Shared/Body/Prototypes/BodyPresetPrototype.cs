using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Prototypes
{
    /// <summary>
    ///     Defines the parts used in a body.
    /// </summary>
    [Prototype("bodyPreset")]
    [Serializable, NetSerializable]
    public sealed class BodyPresetPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [DataField("partIDs")]
        private Dictionary<string, string> _partIDs = new();

        [ViewVariables]
        [DataField("name")]
        public string Name { get; } = string.Empty;

        [ViewVariables]
        public Dictionary<string, string> PartIDs => new(_partIDs);
    }
}
