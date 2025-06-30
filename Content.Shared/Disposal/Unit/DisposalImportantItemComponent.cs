using Robust.Shared.GameStates;

namespace Content.Shared.Disposal.Unit;

/// <summary>
///     Anything that would be at least a LITTLE undesirable to accidentally trash and then maybe lose forever
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DisposalImportantItemComponent : Component
{
    /// <summary>
    ///     What this should be called in the popup.
    ///     If this is not defined, use the item's name instead.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public string? ItsA;

    /// <summary>
    ///     How long it takes before the popup will show up again.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ResetTime = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     Window before you can actually insert the item.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AntiSpamWindow = TimeSpan.FromMilliseconds(500);

    /// <summary>
    ///     Last attempt at inserting an important item into disposals.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastAttempt;
}
