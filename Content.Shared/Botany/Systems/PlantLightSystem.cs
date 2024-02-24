using Content.Shared.Botany.Components;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Plant light requirement isn't implemented, only handles inheriting and mutating values.
/// </summary>
public sealed class PlantLightSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlantLightComponent, PlantCopyTraitsEvent>(OnCopyTraits);
    }

    // TODO: mutation

    private void OnCopyTraits(Entity<PlantLightComponent> ent, ref PlantCopyTraitsEvent args)
    {
        var light = EnsureComp<PlantLightComponent>(args.Plant);
        light.Ideal = ent.Comp.Ideal;
        light.Tolerance = ent.Comp.Tolerance;
    }
}
