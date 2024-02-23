using Content.Shared.Botany.Components;
using Content.Shared.Examine;
using Robust.Shared.Containers;

namespace Content.Shared.Botany.Systems;

public sealed class PlantHolderSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantHolderComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PlantHolderComponent, ExaminedEvent>(OnExamined);
    }

    private void OnStartup(Entity<PlantHolderComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<ContainerManagerComponent>(ent, out var manager))
            return;

        var id = ent.Comp.PlantContainerId;
        ent.Comp.PlantContainer = _container.EnsureContainer<ContainerSlot>(ent, id, manager);
    }

    private void OnExamined(Entity<PlantHolderComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.PlantEntity is {} plant)
            RaiseLocalEvent(plant, ref args);
        else
            args.PushMarkup(Loc.GetString("plant-holder-component-nothing-planted-message"));
    }

    /// <summary>
    /// Tries to plant a seed in the holder, returning true if it is now planted.
    /// </summary>
    public bool TryPlant(Entity<PlantHolderComponent?> holder, Entity<SeedComponent> seed, EntityUid user)
    {
        if (!Resolve(holder, ref holder.Comp))
            return false;

        if (ent.Comp.PlantEntity != null)
        {
            var msg = Loc.GetString("plant-holder-component-already-seeded-message", ("name", Name(holder)));
            _popup.PopupClient(msg, holder, user, user, PopupType.Medium);
            return false;
        }

        // if there's already a plant entity to insert just use it, no deleting
        if (seed.Comp.PlantEntity is {} plant)
            return _container.Insert(plant, ent.Comp.Container);

        // plant prototype must be set if there isn't an entity
        var plant = Spawn(seed.Comp.Plant!);
        if (_container.Insert(plant, ent.Comp.Container))
            return true;

        // plant was just spawned for this so if it failed we must delete it
        Del(plant);
        return false;
    }
}
