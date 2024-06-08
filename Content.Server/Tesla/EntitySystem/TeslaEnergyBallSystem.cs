using Content.Server.ParticleAccelerator.Components;
using Content.Server.Tesla.Components;
using Content.Shared.Singularity.Components;
using Robust.Server.Audio;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;


namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// A component that tracks an entity's saturation level from absorbing other creatures by touch, and spawns new entities when the saturation limit is reached.
/// </summary>
public sealed class TeslaEnergyBallSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeslaEnergyBallComponent, PreventCollideEvent>(OnPreventCollide);
    }

    /// <summary>
    /// only collides with the containment fields, containment field generators, or particle projectile components from the PA
    /// </summary>
    public void OnPreventCollide(Entity<TeslaEnergyBallComponent> entity, ref PreventCollideEvent args)
    {
        if (HasComp<ContainmentFieldComponent>(args.OtherEntity) 
            || HasComp<ContainmentFieldGeneratorComponent>(args.OtherEntity)
            || HasComp<ParticleProjectileComponent>(args.OtherEntity))
            return;

        args.Cancelled = true;
    }

    public void AdjustEnergy(EntityUid uid, TeslaEnergyBallComponent component, float delta)
    {
        component.Energy += delta;

        if (component.Energy > component.NeedEnergyToSpawn)
        {
            component.Energy -= component.NeedEnergyToSpawn;
            Spawn(component.SpawnProto, Transform(uid).Coordinates);
        }
        if (component.Energy < component.EnergyToDespawn)
        {
            _audio.PlayPvs(component.SoundCollapse, uid);
            QueueDel(uid);
        }
    }
}
