using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._EE.Plasmaman;

/// <summary>
/// Lets the plasmaman vent their bodily plasma to dash a short distance in space.
/// Range scales with the wearer's respirator saturation and the action only fires
/// when the surrounding atmosphere is thin enough to count as space.
/// </summary>
[RegisterComponent]
public sealed partial class PlasmamanDashComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionPlasmamanDash";

    [DataField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Maximum throw speed at full saturation. Roughly tiles/second.
    /// </summary>
    [DataField]
    public float MaxStrength = 8f;

    /// <summary>
    /// Minimum throw speed even at low saturation. Used to clamp the lower bound so
    /// the dash isn't pointless when the wearer is barely breathing.
    /// </summary>
    [DataField]
    public float MinStrength = 2f;

    /// <summary>
    /// Required minimum saturation to fire the dash at all.
    /// </summary>
    [DataField]
    public float MinSaturation = 0.5f;

    /// <summary>
    /// Saturation removed when the dash fires. Costs go in proportion of MaxSaturation.
    /// </summary>
    [DataField]
    public float SaturationCost = 2.5f;

    /// <summary>
    /// Above this total-mole count in the surrounding mixture the dash refuses to fire.
    /// 0 means strict vacuum; default allows extremely thin atmospheres.
    /// </summary>
    [DataField]
    public float MaxAtmosMoles = 0.5f;

    [DataField]
    public SoundSpecifier? Sound;
}

public sealed partial class PlasmamanDashEvent : InstantActionEvent { }
