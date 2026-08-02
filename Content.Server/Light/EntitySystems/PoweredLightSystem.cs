using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;

namespace Content.Server.Light.EntitySystems;

/// <inheritdoc/>
public sealed partial class PoweredLightSystem : SharedPoweredLightSystem
{
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<PoweredLightComponent> ent, ref MapInitEvent args)
    {
        // TODO: Use ContainerFill dog
        if (ent.Comp.HasLampOnSpawn != null)
        {
            var entity = Spawn(ent.Comp.HasLampOnSpawn, Transform(ent).Coordinates);
            ContainerSystem.Insert(entity, ent.Comp.LightBulbContainer);
        }
        // need this to update visualizers
        UpdateLight(ent, ent.Comp);
    }
}
