using Robust.Shared.GameStates;

namespace Content.Shared.Cpr;

/// <summary>
/// The entity has their breathing assisted by external means, and can breathe even in crit
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AssistedRespirationComponent : Component
{
    /// <summary>
    /// The time when the assist will expire
    /// It will be removed on the next respiration attempt
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AssistedUntil;
}
