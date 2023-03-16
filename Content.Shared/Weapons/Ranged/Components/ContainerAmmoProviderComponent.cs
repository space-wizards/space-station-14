using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
///     Handles pulling entities from the given container to use as ammunition.
/// </summary>
[RegisterComponent]
public sealed class ContainerAmmoProviderComponent : AmmoProviderComponent
{
    [DataField("container", required: true)]
    public string Container = default!;

    /// <summary>
    /// If true, will throw ammo rather than shoot it.
    /// Used for improvised pneumatic cannon.
    /// </summary>
    [DataField("throwItems"), ViewVariables(VVAccess.ReadWrite)]
    public bool ThrowItems;
}
