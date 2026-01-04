using System.Linq;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Content.Server.Botany.Components;
using Content.Server.Botany.Events;
using Content.Shared.Interaction;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Base system for managing plant traits.
/// </summary>
public sealed class PlantTraitsSystem : EntitySystem
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlantTraitsComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<PlantTraitsComponent, PlantCrossPollinateEvent>(OnCrossPollinate);
        SubscribeLocalEvent<PlantTraitsComponent, OnPlantGrowEvent>(OnPlantGrow);
        SubscribeLocalEvent<PlantTraitsComponent, DoHarvestEvent>(OnDoHarvest);
        SubscribeLocalEvent<PlantTraitsComponent, AfterDoHarvestEvent>(OnAfterDoHarvest);
        SubscribeLocalEvent<PlantTraitsComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlantTraitsComponent, PlantSampleAttemptEvent>(OnPlantSampleAttempt);
    }

    private void OnCrossPollinate(Entity<PlantTraitsComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<PlantTraitsComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossTrait(ref ent, pollenData.Traits);
    }

    private void OnPlantGrow(Entity<PlantTraitsComponent> ent, ref OnPlantGrowEvent args)
    {
        foreach (var trait in ent.Comp.Traits.ToArray())
        {
            trait.OnPlantGrow(ent, ref args);
        }
    }

    private void OnDoHarvest(Entity<PlantTraitsComponent> ent, ref DoHarvestEvent args)
    {
        foreach (var trait in ent.Comp.Traits.ToArray())
        {
            trait.OnDoHarvest(ent, ref args);
        }
    }

    private void OnAfterDoHarvest(Entity<PlantTraitsComponent> ent, ref AfterDoHarvestEvent args)
    {
        foreach (var trait in ent.Comp.Traits.ToArray())
        {
            trait.OnAfterDoHarvest(ent, ref args);
        }
    }

    private void OnInteractUsing(Entity<PlantTraitsComponent> ent, ref InteractUsingEvent args)
    {
        foreach (var trait in ent.Comp.Traits.ToArray())
        {
            trait.OnInteractUsing(ent, ref args);
        }
    }

    private void OnInit(Entity<PlantTraitsComponent> ent, ref ComponentInit args)
    {
        var deps = _entitySystemManager.DependencyCollection;
        foreach (var trait in ent.Comp.Traits)
        {
            deps.InjectDependencies(trait);
        }
    }

    private void OnPlantSampleAttempt(Entity<PlantTraitsComponent> ent, ref PlantSampleAttemptEvent args)
    {
        foreach (var trait in ent.Comp.Traits.ToArray())
        {
            trait.OnPlantSampleAttempt(ent, ref args);
        }
    }

    /// <summary>
    /// Tries to get a trait from the plant traits component.
    /// </summary>
    [PublicAPI]
    public bool TryGetTrait<T>(PlantTraitsComponent component, [NotNullWhen(true)] out T? trait)
        where T : PlantTrait
    {
        trait = null;
        foreach (var existing in component.Traits)
        {
            if (existing is T t)
            {
                trait = t;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Removes a trait from the plant traits component.
    /// </summary>
    [PublicAPI]
    public void DelTrait(Entity<PlantTraitsComponent?> ent, PlantTrait trait)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Traits.RemoveAll(t => t.GetType() == trait.GetType());
    }

    /// <summary>
    /// Adds a trait to the plant traits component.
    /// </summary>
    [PublicAPI]
    public void AddTrait(Entity<PlantTraitsComponent?> ent, PlantTrait trait)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.Traits.Any(t => t.GetType() == trait.GetType()))
            return;

        _entitySystemManager.DependencyCollection.InjectDependencies(trait);
        ent.Comp.Traits.Add(trait);
    }
}

public abstract class PlantTrait : EntitySystem
{
    public virtual void OnPlantGrow(Entity<PlantTraitsComponent> ent, ref OnPlantGrowEvent args)
    {
    }

    public virtual void OnDoHarvest(Entity<PlantTraitsComponent> ent, ref DoHarvestEvent args)
    {
    }

    public virtual void OnAfterDoHarvest(Entity<PlantTraitsComponent> ent, ref AfterDoHarvestEvent args)
    {
    }

    public virtual void OnInteractUsing(Entity<PlantTraitsComponent> ent, ref InteractUsingEvent args)
    {
    }

    public virtual void OnPlantSampleAttempt(Entity<PlantTraitsComponent> ent, ref PlantSampleAttemptEvent args)
    {
    }

    public virtual IEnumerable<string> GetPlantStateMarkup()
    {
        yield break;
    }
}
