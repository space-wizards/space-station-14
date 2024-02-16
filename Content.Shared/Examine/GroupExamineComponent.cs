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
        [DataField]
        public List<ExamineGroup> Group = new()
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
        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public string? Title;

        /// <summary>
        ///     A list of ExamineEntries, containing which component it belongs to, which priority it has, and what FormattedMessage it holds.
        /// </summary>
        [DataField]
        public List<ExamineEntry> Entries = new();

        // TODO custom type serializer, or just make this work via some other automatic grouping process that doesn't
        // rely on manually specifying component names in yaml.
        /// <summary>
        ///     A list of all components this ExamineGroup encompasses.
        /// </summary>
        [DataField]
        public List<string> Components = new();

        /// <summary>
        ///     The icon path for the Examine Group.
        /// </summary>
        [DataField]
        public SpriteSpecifier Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/examine-star.png"));

        /// <summary>
        ///     The text shown in the context verb menu.
        /// </summary>
        [DataField]
        public LocId ContextText = "verb-examine-group-other";

        /// <summary>
        ///     Details shown when hovering over the button.
        /// </summary>
        [DataField]
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
        [DataField(required: true)]
        public string Component;

        /// <summary>
        ///     What priority has this entry - entries are sorted high to low.
        /// </summary>
        [DataField]
        public float Priority = 0f;

        /// <summary>
        ///     The FormattedMessage of this entry.
        /// </summary>
        [DataField(required: true)]
        public FormattedMessage Message;

        /// <param name="component">Should be set to _componentFactory.GetComponentName(component.GetType()) to properly function.</param>
        public ExamineEntry(string component, float priority, FormattedMessage message)
        {
            Component = component;
            Priority = priority;
            Message = message;
        }

        private ExamineEntry()
        {
            // parameterless ctor is required for data-definition serialization
            Message = default!;
            Component = default!;
        }
    }

}
