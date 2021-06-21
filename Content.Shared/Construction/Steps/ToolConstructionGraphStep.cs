#nullable enable
using Content.Shared.Tool;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public class ToolConstructionGraphStep : ConstructionGraphStep
    {
        [DataField("tool")] public ToolQuality Tool { get; } = ToolQuality.None;

        [DataField("fuel")] public float Fuel { get; } = 10;

        [DataField("examine")] public string ExamineOverride { get; } = string.Empty;

        public override void DoExamine(FormattedMessage message, bool inDetailsRange)
        {
            if (!string.IsNullOrEmpty(ExamineOverride))
            {
                message.AddMarkup(Loc.GetString(ExamineOverride));
                return;
            }

            message.AddMarkup(Loc.GetString("construction-use-tool-entity", ("toolName", Tool.GetToolName())));
        }
    }
}
