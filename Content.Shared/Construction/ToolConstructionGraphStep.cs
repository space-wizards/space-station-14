#nullable enable
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    public class ToolConstructionGraphStep : ConstructionGraphStep
    {
        public ToolQuality Tool { get; private set; }
        public float Fuel { get; private set; }
        public string ExamineOverride { get; private set; } = string.Empty;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Tool, "tool", ToolQuality.None);
            serializer.DataField(this, x => x.Fuel, "fuel", 10f); // Default fuel cost.
            serializer.DataField(this, x => x.ExamineOverride, "examine", string.Empty);
        }

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
