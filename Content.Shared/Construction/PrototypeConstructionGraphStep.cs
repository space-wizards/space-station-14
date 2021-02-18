using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    [DataDefinition]
    public class PrototypeConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("prototype")] public string Prototype { get; private set; } = string.Empty;

        public override bool EntityValid(IEntity entity)
        {
            return entity.Prototype?.ID == Prototype;
        }

        public override void DoExamine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(string.IsNullOrEmpty(Name)
                ? Loc.GetString("Next, insert {0}", Prototype) // Terrible.
                : Loc.GetString("Next, insert {0}", Name));
        }
    }
}
