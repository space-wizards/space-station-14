using Robust.Shared.Serialization;
using Robust.Shared.Utility;

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
                Identifier = "armor",
                Title = FormattedMessage.FromMarkup("title_here"),
                Components = new()
                {
                    "Armor",
                    "ClothingSpeedModifier"
                },
            },
        };
    }

    [DataDefinition]
    public sealed class ExamineGroup
    {
        [DataField("id")]
        public string Identifier = string.Empty;

        [DataField("title")]
        public FormattedMessage? Title;

        [DataField("entries")]
        public List<ExamineEntry> Entries = new();

        [DataField("components")]
        public List<string> Components = new();
        
        [DataField("icon")]
        public string Icon = "/Textures/Interface/VerbIcons/dot.svg.192dpi.png";

        [DataField("contextText")]
        public string ContextText = string.Empty; //shown in context menu

        [DataField("hoverMessage")]
        public string HoverMessage = string.Empty; //shown when hovering icon or context menu
    }

    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed class ExamineEntry
    {
        [DataField("component")]
        public string ComponentName = string.Empty;

        [DataField("priority")]
        public float Priority = 0f;

        [DataField("message")]
        public FormattedMessage Message = new();

        public ExamineEntry(string componentName, float priority, FormattedMessage message)
        {
            ComponentName = componentName;
            Priority = priority;
            Message = message;
        }
    }

}
