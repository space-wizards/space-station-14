using Content.Shared.Interaction;
using Content.Shared.Weapons.Misc;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed class GrapplingGunSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GrapplingGunComponent, ActivateInWorldEvent>(OnGrapplingActivate);
    }

    private void OnGrapplingActivate(EntityUid uid, GrapplingGunComponent component, ActivateInWorldEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        _joints.RemoveJoint(uid, GrapplingProjectileSystem.GrapplingJoint);
    }
}
