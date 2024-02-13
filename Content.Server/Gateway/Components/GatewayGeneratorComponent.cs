using Content.Shared.Parallax.Biomes.Markers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Gateway.Components;

/// <summary>
/// Generates gateway destinations at a regular interval.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class GatewayGeneratorComponent : Component
{
    /// <summary>
    /// Prototype to spawn on the generated map if applicable.
    /// </summary>
    [DataField]
    public EntProtoId? Proto = "Gateway";

    /// <summary>
    /// Next time another seed unlocks.
    /// </summary>
    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUnlock;

    /// <summary>
    /// How long it takes to unlock another destination once one is taken.
    /// </summary>
    [DataField]
    public TimeSpan UnlockCooldown = TimeSpan.FromMinutes(75);

    /// <summary>
    /// Maps we've generated.
    /// </summary>
    [DataField]
    public List<EntityUid> Generated = new();

    [DataField]
    public int MobLayerCount = 1;

    /// <summary>
    /// Mob layers to pick from.
    /// </summary>
    [DataField]
    public List<ProtoId<BiomeMarkerLayerPrototype>> MobLayers = new()
    {
        "Carps",
        "Xenos",
    };

    [DataField]
    public int LootLayerCount = 3;

    /// <summary>
    /// Loot layers to pick from.
    /// </summary>
    public List<ProtoId<BiomeMarkerLayerPrototype>> LootLayers = new()
    {
        "OreIron",
        "OreQuartz",
        "OreGold",
        "OreSilver",
        "OrePlasma",
        "OreUranium",
        "OreBananium",
        "OreArtifactFragment",
    };
}

