using Content.Server.Explosion.EntitySystems;
using Content.Server.Lightning.Components;
using Content.Server.Lightning.Events;
using Content.Server.Power.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// The system is responsible for switching the status of the active lightning target
/// </summary>
public sealed class LightningTargetSystem : EntitySystem
{

    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightningTargetComponent, HittedByLightningEvent>(OnHittedByLightning);
    }

    private void OnHittedByLightning(EntityUid uid, LightningTargetComponent component, ref HittedByLightningEvent args)
    {
        Log.Debug("јй, мен€ ударила молни€");
        if (!component.LightningExplode)
            return;
        Log.Debug("придетс€ взрыватьс€");
        _explosionSystem.QueueExplosion(
            uid,
            component.ExplosionPrototype,
            component.TotalIntensity,
            component.Dropoff,
            component.MaxTileIntensity
            );
        Log.Debug("взорвалс€");
    }
}
