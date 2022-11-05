using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.Core.Tokens;

namespace Content.Shared.Examine
{
    /// <summary>
    ///     This component groups examine messages together
    /// </summary>
    [RegisterComponent]
    public sealed class GroupExamineComponent : Component
    {
        [DataField("group")]
        public List<ExamineGroup> ExamineGroups = new()
        {
            new ExamineGroup()
            {
                // Identifier = "armor",
                //Title = FormattedMessage.FromMarkup(Loc.GetString("examine-group-armor-title")),
                Components = new()
                {
                    "Armor",
                    "ClothingSpeedModifier",
                },
            },
        };
    }

    [DataDefinition]
    public sealed class ExamineGroup
    {
        private FormattedMessage? _title = null;

        /// <summary>
        ///     The title of the Examine Group, the first FormattedMessage.
        /// </summary>
        [DataField("title")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FormattedMessage? Title
        {
            get => _title;
            set => _title = (value != null) ? FormattedMessage.FromMarkup(Loc.GetString(value.ToString())) : null;
        }

        /// <summary>
        ///     A list of ExamineEntries, containing which component it belongs to, which priority it has, and what FormattedMessage it holds.
        /// </summary>
        [DataField("entries")]
        public List<ExamineEntry> Entries = new();

        /// <summary>
        ///     A list of all components this ExamineGroup encompasses.
        /// </summary>
        [DataField("components")]
        public List<string> Components = new();

        /// <summary>
        ///     The icon path for the Examine Group.
        /// </summary>
        [DataField("icon")]
        public string Icon = "/Textures/Interface/examine-star.png";

        /// <summary>
        ///     The text shown in the context verb menu.
        /// </summary>
        [DataField("contextText")]
        public string ContextText = string.Empty;

        /// <summary>
        ///     Details shown when hovering over the button.
        /// </summary>
        [DataField("hoverMessage")]
        public string HoverMessage = string.Empty;
    }

    /// <summary>
    ///     An entry used when showing examine details
    /// </summary>
    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed class ExamineEntry
    {
        /// <summary>
        ///     Which component does this entry relate to?
        /// </summary>
        [DataField("component")]
        public string ComponentName = string.Empty;

        /// <summary>
        ///     What priority has this entry - entries are sorted high to low.
        /// </summary>
        [DataField("priority")]
        public float Priority = 0f;

        /// <summary>
        ///     The FormattedMessage of this entry.
        /// </summary>
        [DataField("message")]
        public FormattedMessage Message = new();

        /// <param name="componentName">Should be set to _componentFactory.GetComponentName(component.GetType()) to properly function.</param>
        public ExamineEntry(string componentName, float priority, FormattedMessage message)
        {
            ComponentName = componentName;
            Priority = priority;
            Message = message;
        }
    }

}
