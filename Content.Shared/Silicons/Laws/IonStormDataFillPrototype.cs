using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Silicons.Laws;

/// <summary>
/// A prototype for a reusable set of ion storm law targets.
/// </summary>
[Prototype("IonStormDataFill")]
public sealed partial class IonStormDataFillPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The list of selectors to pick from.
    /// </summary>
    [DataField("targets")]
    public List<IonLawSelector> Targets { get; private set; } = new();
}

/// <summary>
/// Selects a random value from an IonStormDataFill prototype.
/// </summary>
[DataDefinition]
public sealed partial class IonStormDataFill : IonLawSelector
{
    /// <summary>
    /// The IonStormDataFill prototype to use.
    /// </summary>
    [DataField("target")]
    public ProtoId<IonStormDataFillPrototype> Target { get; private set; }

    public override object? Select(IRobustRandom random, IPrototypeManager proto, IEntityManager entManager, HashSet<string>? seenIds = null)
    {
        if (seenIds != null && seenIds.Contains(Target))
            return null;

        if (!proto.TryIndex(Target, out var target))
            return null;

        if (target.Targets.Count == 0)
            return null;

        seenIds ??= new();
        seenIds.Add(Target);

        var selector = IonLawSelector.Pick(random, target.Targets);
        return selector.Select(random, proto, entManager, seenIds);
    }
}
