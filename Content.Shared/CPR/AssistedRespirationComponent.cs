using Robust.Shared.GameStates;

namespace Content.Shared.CPR;

/// <summary>
/// The entity has their breathing assisted by external means, and can breathe even in crit
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AssistedRespirationComponent : Component
{
    /// <summary>
    /// The time when the assist will expire
    /// It will be removed on the next respiration attempt
    /// </summary>
    public TimeSpan AssistedUntil;
}
