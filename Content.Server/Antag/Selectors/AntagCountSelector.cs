using Content.Shared.Antag;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Antag.Selectors;

/// <summary>
/// An abstract class meant to return the amount of antags to spawn.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class AntagCountSelector
{
    /// <summary>
    /// How many players does this antag count as?
    /// Each antag spawned by a game rule "takes" a select group of players from the pool.
    /// </summary>
    [DataField]
    public int PlayerRatio = 10;

    [DataField(required: true)]
    public ProtoId<AntagSpecifierPrototype> Proto;

    public abstract int GetTargetAntagCount(IRobustRandom random, int playerCount);

    public static implicit operator ProtoId<AntagSpecifierPrototype>(AntagCountSelector selector)
    {
        return selector.Proto;
    }
}

/// <summary>
/// An abstract version of <see cref="AntagCountSelector"/> which constrains the amount of antags spawned to a minimum and maximum.
/// </summary>
public abstract partial class MinMaxAntagCountSelector : AntagCountSelector
{
    [DataField(required: true)]
    public MinMax Range;
}
