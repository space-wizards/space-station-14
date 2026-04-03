using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Ensures a gun can not be reloaded faster than the given <see cref="InsertCooldown"/>.
/// Currently compatible with <see cref="BallisticAmmoProviderComponent"/> and <see cref="RevolverAmmoProviderComponent"/>.
/// Is ignored by <see cref="BallisticAmmoSelfRefillerComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), Access(typeof(SharedGunSystem)), AutoGenerateComponentPause]
public sealed partial class AmmoProviderInsertionCooldownComponent : Component
{
    /// <summary>
    /// The minimum time in between insertions.
    /// </summary>
    [DataField]
    public TimeSpan InsertCooldown = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// Last time ammo was inserted into this provider.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastInsertion = TimeSpan.Zero;
}
