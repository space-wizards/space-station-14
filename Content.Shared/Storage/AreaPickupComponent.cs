using Content.Shared.Item;
using Robust.Shared.GameStates;
using Content.Shared.Storage.EntitySystems;

namespace Content.Shared.Storage;

/// <summary>
/// This component enables "area pickup" behavior, which means while holding a storage-like item, clicking in the world
/// will attempt to pick up items near the clicked location with a do-after delay.
/// </summary>
/// <seealso cref="AreaPickupSystem"/>
/// <seealso cref="QuickPickupComponent"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AreaPickupComponent : Component
{
    /// <summary>
    /// Minimum delay between quick/area insert actions.
    /// </summary>
    /// <remarks>Used to prevent autoclickers spamming server with individual pickup actions.</remarks>
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// The pickup radius, in tiles.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Radius = 1;

    /// <summary>
    /// How many entities area pickup can pickup at once.
    /// </summary>
    public const int MaximumPickupLimit = 10;

    /// <summary>
    /// How long the do-after should be per <see cref="ItemSizePrototype.Weight">unit of weight</see> of all of the
    /// items picked up.
    /// </summary>
    public static readonly TimeSpan DelayPerItemWeight = TimeSpan.FromSeconds(0.075);
}
