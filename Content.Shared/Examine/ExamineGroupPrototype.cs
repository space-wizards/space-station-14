using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Prototypes
{
    /// <summary>
    ///     An examine group, used for grouping together certain examine details.
    /// </summary>
    [Prototype("examineGroup")]
    public sealed class ExamineGroupPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; } = default!;

        /// <summary>
        ///     Icon for the button that shows this examine group.
        /// </summary>
        [DataField("icon")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string Icon { get; set; } = "/Textures/Interface/VerbIcons/dot.svg.192dpi.png";

        /// <summary>
        ///     The first line of the examine group, presenting the details shown.
        /// </summary>
        [DataField("firstLine")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string FirstLine { get; set; } = string.Empty;

        /// <summary>
        ///     Text shown when right clicking to examine this specific group.
        /// </summary>
        [DataField("text")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        ///     Text shown when hovering over the button to examine this specific group.
        /// </summary>
        [DataField("message")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Interface for components that should show their info in an examine group.
    /// </summary>
    public interface IExamineGroup
    {
        /// <summary>
        ///     The string identifier for the examine group.
        /// </summary>
        public string ExamineGroup { get; set; }

        /// <summary>
        ///     The priority of the component details in the examine group - used for sorting.
        /// </summary>
        public float ExaminePriority { get; set; }
    }
}
