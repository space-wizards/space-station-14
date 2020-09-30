using Content.Shared.GameObjects.Components.Interactable;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    public class ToolConstructionGraphStep : ConstructionGraphStep
    {
        public ToolQuality Tool { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Tool, "tool", ToolQuality.None);
        }

        public override void DoExamine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("Next, use {0:a} {0} tool.", Tool));
        }
    }
}
