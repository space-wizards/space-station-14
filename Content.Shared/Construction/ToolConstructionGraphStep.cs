using Content.Shared.GameObjects.Components.Interactable;
using Robust.Shared.Serialization;

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
    }
}
