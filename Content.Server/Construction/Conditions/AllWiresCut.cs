using Content.Server.Wires;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions
{
    /// <summary>
    ///     A condition that requires all wires to be cut (or intact)
    ///     Returns true if the entity doesn't have a wires component.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class AllWiresCut : IGraphCondition
    {
        [DataField("value")] public bool Value { get; private set; } = true;

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(uid, out WiresComponent? wires))
                return true;

            foreach (var wire in wires.WiresList)
            {
                switch (Value)
                {
                    case true when !wire.IsCut:
                    case false when wire.IsCut:
                        return false;
                }
            }

            return true;
        }

        public bool DoExamine(ExaminedEvent args)
        {
            if (Condition(args.Examined, IoCManager.Resolve<IEntityManager>()))
                return false;

            args.PushMarkup(Loc.GetString(Value
                ? "construction-examine-condition-all-wires-cut"
                : "construction-examine-condition-all-wires-intact"));
            return true;
        }

        public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry()
            {
                Localization = Value ? "construction-guide-condition-all-wires-cut"
                    : "construction-guide-condition-all-wires-intact"
            };
        }
    }
}
