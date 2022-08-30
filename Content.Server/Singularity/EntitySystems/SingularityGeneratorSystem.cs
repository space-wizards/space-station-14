using Content.Server.ParticleAccelerator.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Singularity.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;

namespace Content.Server.Singularity.EntitySystems;

public sealed class SingularityGeneratorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParticleProjectileComponent, StartCollideEvent>(HandleParticleCollide);
    }

    private void HandleParticleCollide(EntityUid uid, ParticleProjectileComponent component, ref StartCollideEvent args)
    {
        if (EntityManager.TryGetComponent<SingularityGeneratorComponent?>(args.OtherFixture.Body.Owner, out var singularityGeneratorComponent))
        {
            singularityGeneratorComponent.Power += component.State switch
            {
                ParticleAcceleratorPowerState.Standby => 0,
                ParticleAcceleratorPowerState.Level0 => 1,
                ParticleAcceleratorPowerState.Level1 => 2,
                ParticleAcceleratorPowerState.Level2 => 4,
                ParticleAcceleratorPowerState.Level3 => 8,
                _ => 0
            };
            EntityManager.QueueDeleteEntity(uid);
        }
    }
}
