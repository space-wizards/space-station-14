using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

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
    [DataField("maps", customTypeSerializer:typeof(PrototypeIdHashSetSerializer<GameMapPrototype>), required: true)]
    public HashSet<string> Maps = new(0);
}
