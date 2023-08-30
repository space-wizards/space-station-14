using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Examine
{
    /// <summary>
    ///     This component groups examine messages together
    /// </summary>
    [RegisterComponent]
    public sealed partial class GroupExamineComponent : Component
    {
        /// <summary>
        ///     A list of ExamineGroups.
        /// </summary>
        [DataField("group")]
        public List<ExamineGroup> ExamineGroups = new()
        {
            // TODO Remove hardcoded component names.
            new ExamineGroup()
            {
                Components = new()
                {
                    "Armor",
                    "ClothingSpeedModifier",
                },
            },
        };
    }

    [DataDefinition]
    public sealed partial class ExamineGroup
    {
        /// <summary>
        ///     The title of the Examine Group. Localized string that gets added to the examine tooltip.
        /// </summary>
        [DataField("title")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string? Title;

        /// <summary>
        ///     A list of ExamineEntries, containing which component it belongs to, which priority it has, and what FormattedMessage it holds.
        /// </summary>
        [DataField("entries")]
        public List<ExamineEntry> Entries = new();

        // TODO custom type serializer, or just make this work via some other automatic grouping process that doesn't
        // rely on manually specifying component names in yaml.
        /// <summary>
        ///     A list of all components this ExamineGroup encompasses.
        /// </summary>
        [DataField("components")]
        public List<string> Components = new();

        /// <summary>
        ///     The icon path for the Examine Group.
        /// </summary>
        [DataField("icon")]
        public SpriteSpecifier Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/examine-star.png"));

        /// <summary>
        ///     The text shown in the context verb menu.
        /// </summary>
        [DataField("contextText")]
        public string ContextText = "verb-examine-group-other";

        /// <summary>
        ///     Details shown when hovering over the button.
        /// </summary>
        [DataField("hoverMessage")]
        public string HoverMessage = string.Empty;
    }

    /// <summary>
    ///     An entry used when showing examine details
    /// </summary>
    [Serializable, NetSerializable, DataDefinition]
    public sealed partial class ExamineEntry
    {
        /// <summary>
        ///     Which component does this entry relate to?
        /// </summary>
        [DataField("component", required: true)]
        public string ComponentName;

        /// <summary>
        ///     What priority has this entry - entries are sorted high to low.
        /// </summary>
        [DataField("priority")]
        public float Priority = 0f;

        /// <summary>
        ///     The FormattedMessage of this entry.
        /// </summary>
        [DataField("message", required: true)]
        public FormattedMessage Message;

        /// <param name="componentName">Should be set to _componentFactory.GetComponentName(component.GetType()) to properly function.</param>
        public ExamineEntry(string componentName, float priority, FormattedMessage message)
        {
            ComponentName = componentName;
            Priority = priority;
            Message = message;
        }

        private ExamineEntry()
        {
            // parameterless ctor is required for data-definition serialization
            Message = default!;
            ComponentName = default!;
        }
    }

}
