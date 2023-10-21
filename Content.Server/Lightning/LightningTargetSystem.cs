using Content.Server.Explosion.EntitySystems;
using Content.Server.Lightning;
using Content.Server.Lightning.Components;

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// The component allows lightning to strike this target. And determining the behavior of the target when struck by lightning.
/// </summary>
public sealed class LightningTargetSystem : EntitySystem
{

    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightningTargetComponent, HittedByLightningEvent>(OnHittedByLightning, after: new[] { typeof(LightningSystem)});
    }

    private void OnHittedByLightning(EntityUid uid, LightningTargetComponent component, ref HittedByLightningEvent args)
    {

        if (!component.LightningExplode)
            return;

        //For some unknown reason it gives a random server crash.
        //To do: make the explosion dependent on the component parameters.
        //_explosionSystem.QueueExplosion(Transform(uid).MapPosition, component.ExplosionPrototype, component.TotalIntensity, component.Dropoff, component.MaxTileIntensity);
        Log.Debug("BOOM на " + Transform(uid).MapPosition.ToString());
    }
}
