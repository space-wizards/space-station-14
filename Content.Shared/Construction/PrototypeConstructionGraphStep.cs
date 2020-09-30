using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    public class PrototypeConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        public string Prototype { get; private set; }
        public string Name { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Prototype, "prototype", string.Empty);
            serializer.DataField(this, x => x.Name, "name", string.Empty);
        }

        public override void DoExamine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(string.IsNullOrEmpty(Name)
                ? Loc.GetString("Next, insert {0}", Prototype) // Terrible.
                : Loc.GetString("Next, insert {0}", Name));
        }
    }
}
