using Content.Server.Tesla.Components;
using Robust.Server.Audio;


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
