using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Worldgen.Prototypes;

/// <summary>
///     This is a prototype for controlling overall world generation.
///     The components included are applied to the map that world generation is configured on.
/// </summary>
[Prototype("worldgenConfig")]
public sealed partial class WorldgenConfigPrototype : IPrototype
{
    /// <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The components that get added to the target map.
    /// </summary>
    [DataField("components", required: true)]
    public ComponentRegistry Components { get; private set; } = default!;

    //TODO: Get someone to make this a method on componentregistry that does it Correctly.
    /// <summary>
    ///     Applies the worldgen config to the given target (presumably a map.)
    /// </summary>
    public void Apply(EntityUid target, ISerializationManager serialization, IEntityManager entityManager)
    {
        // Add all components required by the prototype. Engine update for this whenst.
        foreach (var data in Components.Values)
        {
            var comp = (Component) serialization.CreateCopy(data.Component, notNullableOverride: true);
            entityManager.AddComponent(target, comp);
        }
    }
}

