
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Silicons.Laws;

/// <summary>
/// A prototype for a reusable set of ion storm law targets.
/// </summary>
[Prototype("IonLawTarget")]
public sealed partial class IonLawTargetPrototype : IPrototype
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
/// Selects a random value from an IonLawTarget prototype.
/// </summary>
[DataDefinition]
public sealed partial class IonLawTarget : IonLawSelector
{
    /// <summary>
    /// The IonLawTarget prototype to use.
    /// </summary>
    [DataField("target")]
    public ProtoId<IonLawTargetPrototype> Target { get; private set; }

    public override object? Select(IRobustRandom random, IPrototypeManager proto, IEntityManager entManager)
    {
        if (!proto.TryIndex(Target, out var target))
            return null;

        if (target.Targets.Count == 0)
            return null;

        var selector = IonLawSelector.Pick(random, target.Targets);
        return selector.Select(random, proto, entManager);
    }
}
