using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed class PrototypeConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("prototype")] public string Prototype { get; } = string.Empty;

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager)
        {
            return entityManager.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID == Prototype;
        }

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            examinedEvent.Message.AddMarkup(string.IsNullOrEmpty(Name)
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
