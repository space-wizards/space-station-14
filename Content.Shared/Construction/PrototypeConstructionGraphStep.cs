using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    public class PrototypeConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        public string Prototype { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Prototype, "prototype", string.Empty);
        }

        public override bool EntityValid(IEntity entity)
        {
            return entity.Prototype?.ID == Prototype;
        }

        public override void DoExamine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(string.IsNullOrEmpty(Name)
                ? Loc.GetString(
                    "construction-insert-prototype-no-name",
                    ("prototypeName", Prototype) // Terrible.
                )
                : Loc.GetString(
                    "construction-insert-prototype",
                    ("entityName", Name)
                ));
        }
    }
}
