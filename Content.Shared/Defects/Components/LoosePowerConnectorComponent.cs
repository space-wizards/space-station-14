using Robust.Shared.GameStates;

namespace Content.Shared.Defects.Components;

// Gives a toggleable weapon a chance to deactivate on each swing.
// Flavor: a loose internal power connector that can't hold contact under impact.
// After deactivating, the weapon automatically reactivates after ReactivateDelay.
[RegisterComponent, NetworkedComponent]
public sealed partial class LoosePowerConnectorDefectComponent : DefectComponent
{
    public LoosePowerConnectorDefectComponent()
    {
        DefectLabel = "faulty power connector";
    }

    // Per-swing probability of the weapon powering off.
    [DataField]
    public float PowerFailChance = 0.15f;

    // How long after deactivation before the weapon automatically reactivates.
    [DataField]
    public TimeSpan ReactivateDelay = TimeSpan.FromSeconds(0.5);

    // Set server-side when deactivated; cleared on reactivation. Not networked.
    public TimeSpan? ReactivateAt;
}
