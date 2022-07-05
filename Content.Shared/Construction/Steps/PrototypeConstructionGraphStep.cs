using Content.Shared.Examine;
using Content.Shared.Stacks;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed class PrototypeConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("prototype")] public string Prototype { get; } = string.Empty;
        [DataField("amount")] public int Amount { get; } = 1;

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager)
        {
            return entityManager.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID == Prototype;
        }

        public bool EntityValid(EntityUid entity, out SharedStackComponent? stack)
        {
            stack = null;
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out SharedStackComponent? otherStack) && otherStack.StackTypeId == Prototype)
            {
                stack = otherStack;
                return true;
            }
            else if (IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity).EntityPrototype?.ID == Prototype)
            {
                return true;
            }

           return false;
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

        public override ConstructionGuideEntry GenerateGuideEntry()
        {
            if (Amount > 1)
            {
                return new ConstructionGuideEntry()
                {
                    Localization = "construction-presenter-prototype-amount-step",
                    Arguments = new (string, object)[] { ("amount", Amount), ("name", Name) },
                    Icon = Icon,
                };
            }
            else
            {
                return new ConstructionGuideEntry()
                {
                    Localization = "construction-presenter-arbitrary-step",
                    Arguments = new (string, object)[] { ("name", Name) },
                    Icon = Icon,
                };
            }
        }
    }
}
