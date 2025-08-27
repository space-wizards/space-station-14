using Content.Shared.Storage.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage;

/// <summary>
/// This component enables "quick pickup" behavior, which means while holding a storage-like item, clicking on another
/// item will attempt to insert the clicked item into the held one.
/// </summary>
/// <seealso cref="QuickPickupSystem"/>
/// <seealso cref="AreaPickupComponent"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class QuickPickupComponent : Component
{
    /// <summary>
    /// Minimum delay between quick/area insert actions.
    /// </summary>
    /// <remarks>Used to prevent autoclickers spamming server with individual pickup actions.</remarks>
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(0.5);
}
