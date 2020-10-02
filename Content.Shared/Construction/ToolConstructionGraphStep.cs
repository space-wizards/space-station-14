using System;
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    public class ToolConstructionGraphStep : ConstructionGraphStep
    {
        public ToolQuality Tool { get; private set; }
        public string ExamineOverride { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Tool, "tool", ToolQuality.None);
            serializer.DataField(this, x => x.ExamineOverride, "examine", string.Empty);
        }

        public override void DoExamine(FormattedMessage message, bool inDetailsRange)
        {
            if (!string.IsNullOrEmpty(ExamineOverride))
            {
                message.AddMarkup(Loc.GetString(ExamineOverride));
                return;
            }

            switch (Tool)
            {
                case ToolQuality.Anchoring:
                    message.AddMarkup(Loc.GetString("Next, use a [color=cyan]wrench[/color]."));
                    break;
                case ToolQuality.Prying:
                    message.AddMarkup(Loc.GetString("Next, use a [color=cyan]crowbar[/color]."));
                    break;
                case ToolQuality.Screwing:
                    message.AddMarkup(Loc.GetString("Next, use a [color=cyan]screwdriver[/color]."));
                    break;
                case ToolQuality.Cutting:
                    message.AddMarkup(Loc.GetString("Next, use some [color=cyan]wirecutters[/color]."));
                    break;
                case ToolQuality.Welding:
                    message.AddMarkup(Loc.GetString("Next, use a [color=cyan]welding tool[/color]."));
                    break;
                case ToolQuality.Multitool:
                    message.AddMarkup(Loc.GetString("Next, use a [color=cyan]multitool[/color]."));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
