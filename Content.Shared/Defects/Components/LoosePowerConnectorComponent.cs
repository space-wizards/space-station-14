using Robust.Shared.GameStates;

namespace Content.Shared.Defects.Components;

/// <summary>
/// Gives a toggleable weapon a chance to deactivate on each swing.
/// Flavor: a loose internal power connector that can't hold contact under impact.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LoosePowerConnectorComponent : DefectComponent
{
    /// <summary>Per-swing probability (0–1) of the weapon powering off.</summary>
    [DataField]
    public float PowerFailChance = 0.15f;
}
