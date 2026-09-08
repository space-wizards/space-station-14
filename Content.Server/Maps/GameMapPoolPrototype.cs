using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Maps;

/// <summary>
/// Prototype that holds a pool of maps that can be indexed based on the map pool CCVar.
/// </summary>
[Prototype, PublicAPI]
public sealed partial class GameMapPoolPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Which maps are in this pool.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<GameMapPrototype>> Maps = new(0);
}
