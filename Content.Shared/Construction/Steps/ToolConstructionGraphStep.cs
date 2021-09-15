using Content.Shared.Examine;
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

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            if (!string.IsNullOrEmpty(ExamineOverride))
            {
                examinedEvent.Message.AddMarkup(Loc.GetString(ExamineOverride));
                return;
            }

            examinedEvent.Message.AddMarkup(Loc.GetString("construction-use-tool-entity", ("toolName", Tool.GetToolName())));
        }
    }
}
