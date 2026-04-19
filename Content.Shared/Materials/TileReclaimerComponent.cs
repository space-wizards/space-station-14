using Content.Shared.Maps;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Materials;

/// <summary>
/// Makes an entity into a grid reclaimer, able to delete grids in front of it and produce materials.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedTileReclaimerSystem))]
public sealed partial class TileReclaimerComponent : Component
{
    /// <summary>
    /// Whether or not the machine has power.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Powered;

    /// <summary>
    /// An "enable" toggle for things like interfacing with machine linking.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// A master control for whether or not the reclaimer is broken and can function.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Broken;

    /// <summary>
    /// A whitelist for what grid entities can be affected by this reclaimer.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// A whitelist for what grid entities can be affected by this reclaimer.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// The delay after a grid has been consumed that it can recycle again.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RecycleDelay = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// The bounding box in local coordinates for where it will check for grids to reclaim.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Box2 RecyclingBox = new(-0.5f, 0.5f, 0.5f, 1.5f);

    /// <summary>
    /// Multiplier for the material being extracted from the tile, as defined in the tile's <see cref="ContentTileDefinition.MaterialComposition"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Efficiency = 1f;

    /// <summary>
    /// How strong impulse the reclaimer should apply to a grid after it has destroyed one of it tiles.
    /// This helps guide the grid further into the reclaimer.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SlurpStrength = 1f;

    /// <summary>
    /// Next time a recycling attempt can be made.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField(customTypeSerializer:typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextRecycle;

    /// <summary>
    /// The sound played when something is being processed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound;
}
