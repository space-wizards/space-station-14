using Robust.Shared.Serialization;

namespace Content.Shared.Construction
{
    public class PrototypeConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        public string Prototype { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Prototype, "prototype", string.Empty);
        }
    }
}
