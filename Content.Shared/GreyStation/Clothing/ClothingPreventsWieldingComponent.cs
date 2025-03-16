using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.GreyStation.Clothing;

/// <summary>
/// Prevents this item being wielded if any clothing that match some whitelists is worn.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ClothingPreventsWieldingSystem))]
public sealed partial class ClothingPreventsWieldingComponent : Component
{
    /// <summary>
    /// A dictionary of slots and a whitelist that, if the equipped item matches, prevents wielding.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, EntityWhitelist> Slots = new();
}
