using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared.DamageOverlay;

/// <summary>
/// A component to add overlays to the screen of the controlling player depending on the damage the entity has taken.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedDamageOverlaySystem))]
public sealed partial class DamageOverlayComponent : Component
{
    /// <summary>
    /// The mobstate the overlay currently shows.
    /// </summary>
    [DataField, AutoNetworkedField]
    public MobState State = MobState.Alive;

    /// <summary>
    /// Controls the red vignette around the screen, which closes in as you take damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PainLevel;

    /// <summary>
    /// Controls the white vignette around the screen, which closes the closer you are to death.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CritLevel;

    /// <summary>
    /// Used for lerping the white overlay from <see cref="CritLevel"/> when the entity dies.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DeadLevel;

    /// <summary>
    /// Darkens your screen around the edges based on how much asphyxiation damage you have.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float OxygenLevel;

    /// <summary>
    /// Prevents updates to the overlay from being done by events.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Locked; // For debugging :)
}
