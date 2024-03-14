using Robust.Shared.GameStates;

namespace Content.Shared.Sound.Components;

/// <summary>
/// Prevents an entity from emitting sounds.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SilencedComponent : Component
{
    /// <summary>
    /// Can this entity snore while sleeping?
    /// </summary>
    public bool AllowSnoring;

    /// <summary>
    /// Can this entity make sounds while eating?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AllowEatingSounds;

    /// <summary>
    /// Can this entity make sounds while drinking?
    /// </summary>

    [DataField, AutoNetworkedField]
    public bool AllowDrinkingSounds;
}
