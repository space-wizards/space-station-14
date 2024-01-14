using Robust.Shared.Audio;

namespace Content.Shared.Weapons.Ranged.Events;

[ByRefEvent]
public record struct GunRefreshModifiersEvent(
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
