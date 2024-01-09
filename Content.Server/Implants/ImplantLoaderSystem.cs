using System.Linq;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Implants;

/// <summary>
/// ImplantLoaderSystem covers the server behaviour of handling the loading of implanters with new charges.
/// </summary>
public sealed class ImplantLoaderSystem : SharedImplantLoaderSystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainers = default!;
    [Dependency] private readonly SharedImplanterSystem _implanter = default!;
    [Dependency] private readonly EntityManager _entity = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ImplantLoaderComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ImplantLoaderComponent, MeleeHitEvent>(OnAttack);
    }

    protected override void OnSolutionChange(EntityUid uid, ImplantLoaderComponent component, SolutionContainerChangedEvent args)
    {
        base.OnSolutionChange( uid,  component, args);

        _audio.PlayPvs(component.Refill, uid, new AudioParams().WithVolume(35));
    }

    public void OnAfterInteract(EntityUid uid, ImplantLoaderComponent component, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach)
            return;

        // Do nothing if we're not actually targeting anything
        if (!args.Target.HasValue)
        {
            return;
        }

        if (TryComp<ImplanterComponent>(args.Target.Value, out var implanter))
        {
            _audio.PlayPvs(TryDoInject(component, implanter, args.Target.Value) ? component.Success : component.Failure, uid, new AudioParams().WithVolume(35));
        }
    }

    public void OnAttack(EntityUid uid, ImplantLoaderComponent component, MeleeHitEvent args)
    {
        if (args.Handled)
            return;

        if (!args.HitEntities.Any())
            return;

        if (TryComp<ImplanterComponent>(args.HitEntities.First(), out var implanter))
        {
            _audio.PlayPvs(TryDoInject(component, implanter, args.HitEntities.First()) ? component.Success : component.Failure, uid, new AudioParams().WithVolume(35));
        }
    }

    public bool TryDoInject(ImplantLoaderComponent loader, ImplanterComponent implanter, EntityUid target)
    {
        if (!_implanter.IsImplanterEmpty(target, implanter))
            return false;

        var entity = MakeImplantFromSolution(loader);

        return entity.HasValue && _implanter.LoadImplant(target, implanter, entity.Value);
    }

    private EntityUid? MakeImplantFromSolution(ImplantLoaderComponent loader)
    {
        _solutionContainers.TryGetSolution(loader.Owner, loader.SolutionName, out var solution);
        if (!solution.HasValue)
            return null;

        var contents = solution.Value.Comp.Solution.Contents;

        foreach (var recipeProto in loader.Recipes)
        {
            if (!_prototypeManager.TryIndex(recipeProto, out var recipe))
                continue;

            foreach (var content in contents.Where(content => content.Reagent.ToString().Equals(recipe.Reagent) && content.Quantity >= recipe.CostPerUse))
            {
                solution.Value.Comp.Solution.RemoveReagent(content.Reagent, recipe.CostPerUse);

                return _entity.Spawn(recipe.Product);
            }
        }

        return null;
    }
}
