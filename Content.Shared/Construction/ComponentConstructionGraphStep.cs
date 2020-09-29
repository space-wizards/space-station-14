using Robust.Shared.Serialization;

namespace Content.Shared.Construction
{
    public class ComponentConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        public string Component { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Component, "component", string.Empty);
        }
    }
}
