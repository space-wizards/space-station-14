using Content.Shared.Decals;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Fluids.Components;

/// <summary>
/// Tracks blood picked up by feet or footwear and configures the footprints they leave behind.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class FootstepTrackComponent : Component
{
    /// <summary>
    /// Number of footprint placements it takes for fully bloodied soles to fade out.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ushort MaxSteps = 8;

    /// <summary>
    /// Footprint placements remaining before this tracker is clean.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ushort StepsRemaining;

    /// <summary>
    /// Color of the blood currently carried by this tracker.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color BloodColor = Color.Red;

    /// <summary>
    /// Lowest alpha used by visible footprints.
    /// </summary>
    [DataField, AutoNetworkedField]
    public byte MinimumFootprintAlpha = 20;

    /// <summary>
    /// Decals used for visible footprints. The tracker advances through them as steps are consumed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<DecalPrototype>[] Footprints = ["BloodFootprint1", "BloodFootprint2"];

    /// <summary>
    ///  Index into <see cref="Footprints"/> for the next decal entry.
    /// </summary>
    [DataField, AutoNetworkedField]
    public byte NextFootprintIndex;

    [DataField, AutoNetworkedField]
    public EntityUid LastGrid;

    [DataField, AutoNetworkedField]
    public Vector2i? LastTile;

    [ViewVariables]
    public bool HasLastTile => LastTile.HasValue;
}
