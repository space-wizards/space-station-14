using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage.Prototypes
{
    /// <summary>
    ///     An examine group, used for grouping together certain examine details.
    /// </summary>
    [Prototype("examineGroup")]
    [Serializable, NetSerializable]
    public sealed class ExamineGroupPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; } = default!;

        [DataField("icon")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string Icon { get; set; } = "/Textures/Interface/VerbIcons/dot.svg.192dpi.png";

        [DataField("firstLine")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string FirstLine { get; set; } = string.Empty;

        [DataField("text")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string Text { get; set; } = string.Empty;

        [DataField("message")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string Message { get; set; } = string.Empty;
    }
}
