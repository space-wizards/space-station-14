using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Materials;

/// <summary>
/// Makes an entity into a grid reclaimer, able to delete grids in a square in front of it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(SharedTileReclaimerSystem))]
public sealed partial class TileReclaimerComponent : Component
{
    /// <summary>
    /// A whitelist for what grid entities can be affected by this reclaimer
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// A whitelist for what grid entities can be affected by this reclaimer
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// The delay after a grid has been consumed that it can recycle again.
    /// </summary>
    [DataField]
    public TimeSpan RecycleDelay = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Next time a recycling attempt can be made.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField(customTypeSerializer:typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextRecycle;

    /// <summary>
    /// The sound played when something is being processed.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;
}
