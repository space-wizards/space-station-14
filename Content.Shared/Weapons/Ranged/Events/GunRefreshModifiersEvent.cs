using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
///     Raised directed on the gun entity when <see cref="SharedGunSystem.RefreshModifiers"/>
///     is called, to update the values of <see cref="GunComponent"/> from other systems.
/// </summary>
[ByRefEvent]
public record struct GunRefreshModifiersEvent(
    Entity<GunComponent> Gun,
    SoundSpecifier? SoundGunshot,
    float CameraRecoilScalar,
    Angle AngleIncrease,
    Angle AngleDecay,
    Angle MaxAngle,
    Angle MinAngle,
    int ShotsPerBurst,
    float FireRate,
    float ProjectileSpeed
);
