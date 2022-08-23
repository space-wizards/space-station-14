using Content.Shared.Examine;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed class PrototypeConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("prototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>), required:true)]
        public string Prototype { get; } = string.Empty;

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
