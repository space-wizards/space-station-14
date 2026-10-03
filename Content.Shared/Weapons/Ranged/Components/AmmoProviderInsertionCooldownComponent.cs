using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Ensures a gun can not be reloaded faster than the given <see cref="InsertCooldown"/>.
/// Currently compatible with <see cref="BallisticAmmoProviderComponent"/> and <see cref="RevolverAmmoProviderComponent"/>.
/// Is ignored by <see cref="BallisticAmmoSelfRefillerComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedGunSystem))]
public sealed partial class AmmoProviderInsertionCooldownComponent : Component
{
    /// <summary>
    /// The minimum time in between insertions.
    /// </summary>
    [DataField]
    public TimeSpan InsertCooldown = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// The <see cref="UseDelayComponent.Delays"/> key used for the insertion UseDelay.
    /// </summary>
    [DataField]
    public string UseDelayId = SharedGunSystem.InsertionCooldownId;
}
