using Robust.Shared.GameStates;

namespace Content.Shared.CF.Actions;

/// <summary>
/// Action stamina. One action spends <see cref="StaminaPerAction"/> points.
/// A point returns every <see cref="RegenSeconds"/>. <see cref="MaxActions"/> is per creature,
/// so haste or a potion can raise it past the usual three.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFActionPoolComponent : Component
{
    /// <summary>How many actions this creature can hold.</summary>
    [DataField, AutoNetworkedField]
    public int MaxActions = 3;

    /// <summary>Stamina spent to take one action.</summary>
    [DataField, AutoNetworkedField]
    public float StaminaPerAction = 1f;

    /// <summary>
    /// Stamina available now. Negative means "start full" and is filled on map init.
    /// Full is <see cref="MaxActions"/> times <see cref="StaminaPerAction"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Stamina = -1f;

    /// <summary>Seconds to regain one point of stamina.</summary>
    [DataField, AutoNetworkedField]
    public float RegenSeconds = 1.8f;

    /// <summary>
    /// Strikes and spell attacks already made since this pool was last full.
    /// A cantrip or other action with no attack roll does not increase it.
    /// The next strike takes −5/−10, or −4/−8 when the weapon is agile.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MapAttacks;

    /// <summary>Time accumulated toward the next point. Not networked; the server owns regen.</summary>
    [DataField]
    public float RegenProgress;

    /// <summary>Local throttle so a held click does not stack empty-pool popups.</summary>
    public TimeSpan NextEmptyPopup;
}
