using System.Linq;
using Content.Server.Wires;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.Reflection;

namespace Content.Server.Construction.Conditions
{
    /// <summary>
    ///     A condition that requires all wires to be cut (or intact)
    ///     Returns true if the entity doesn't have a wires component.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed class AllWiresCut : IGraphCondition
    {
        [DataField("value")] public bool Value { get; private set; } = true;

        [DataField("ignoreTypes")] public HashSet<IWireAction> IgnoreTypes { get; } = new();

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(uid, out WiresComponent? wires))
                return true;

            var ignoreTypes = IgnoreTypes.Select(t => t.GetType()).ToHashSet();

            foreach (var wire in wires.WiresList)
            {
                if (ignoreTypes.Contains(wire.Action.GetType()))
                {
                    continue;
                }

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
