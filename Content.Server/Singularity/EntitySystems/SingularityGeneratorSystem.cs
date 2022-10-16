using Content.Server.ParticleAccelerator.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Singularity.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server.Singularity.EntitySystems;

public sealed class SingularityGeneratorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParticleProjectileComponent, StartCollideEvent>(HandleParticleCollide);
    }

    /// <summary>
    /// Sets the amount of power a singularity generator contains.
    /// If the singularity generator has passed its threshold it also spawns a singularity.
    /// </summary>
    /// <param name="comp">The singularity generator component.</param>
    /// <param name="value">The new power level for the generator component to have.</param>
    public void SetPower(SingularityGeneratorComponent comp, int value)
    {
        if (value == comp._power)
            return;

        comp._power = value;
        if (value > comp.Threshold)
            EntityManager.SpawnEntity(comp.SpawnId, Transform(comp.Owner).Coordinates);
    }

    /// <summary>
    /// Handles PA Particles colliding with a singularity generator.
    /// Adds the power from the particles to the generator.
    /// </summary>
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
