using Robust.Shared.GameStates;

namespace Content.Shared.Kitchen.Components;

/// <summary>
/// Used to mark entities that are currently hooked on the spike.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedKitchenSpikeSystem))]
public sealed partial class KitchenSpikeHookedComponent : Component
{
    /// <summary>
    /// The EntityUid of the Kitchen Spike we're hooked to.
    /// </summary>
    [DataField]
    public EntityUid KitchenSpike;
}
