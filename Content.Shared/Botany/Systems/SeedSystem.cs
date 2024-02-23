using Content.Shared.Botany.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles seed interaction with plant holders.
/// </summary>
public sealed class SeedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SeedComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnExamined(Entity<SeedComponent> ent, ref ExaminedEvent args)
    {
        var plant = GetPlant(ent);
    }

    private void OnAfterInteract(Entity<SeedComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !TryComp<PlantHolderComponent>(args.Target, out var holder))
            return;

        args.Handled = true;
        if (_holder.TryPlant((args.Target, holder), ent, args.User))
            QueueDel(ent);
    }

    /// <summary>
    /// Get the required plant component of the seed's plant.
    /// Returning null means a programmer error, either a plant prototype was not specified or it had no <c>PlantComponent</c>.
    /// In both cases this is a skill issue from whoever is making seed or plant prototypes.
    /// </summary>
    public PlantComponent? GetPlant(Entity<SeedComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        if (TryComp<PlantComponent>(ent.Comp.PlantEntity, out var plant))
            return plant;

        if (ent.Comp.Plant is not {} proto)
            return null;

        var proto = _proto.Index<EntityPrototype>(proto);
        return proto.Components.GetValueOrNull("Plant")?.Component;
    }
}
