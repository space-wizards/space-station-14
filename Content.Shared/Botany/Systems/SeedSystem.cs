using Content.Shared.Botany.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using System.Diagnostics.CodeAnalysis;

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
    /// Get a component of the seed's plant entity or prototype.
    /// </summary>
    public bool GetSeedComp<T>(Entity<SeedComponent> ent, [NotNullWhen(true)] out T? comp)
    {
        return GetSeedComp<T>(ent.Comp.Seed, out comp);
    }

    /// <summary>
    /// Get a component of a seed's plant entity or prototype.
    /// </summary>
    public bool GetSeedComp<T>(SeedData seed, [NotNullWhen(true)] out T? comp)
    {
        if (TryComp<T>(seed.Entity, out var entComp))
        {
            comp = entComp;
            return true;
        }

        comp = null;
        if (seed.Plant is not {} proto)
            return false;

        var proto = _proto.Index<EntityPrototype>(proto);
        return proto.TryGetComponent<T>(out comp);
    }
}
