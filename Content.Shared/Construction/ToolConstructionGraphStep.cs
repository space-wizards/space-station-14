#nullable enable
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
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

            message.AddMarkup(Loc.GetString($"Next, use a [color=cyan]{Tool.GetToolName()}[/color]."));
        }
    }
}
