using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Server.GameObjects;
using System.Linq;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class InjectionArtifactSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private EntityQuery<InjectableSolutionComponent> _injectableQuery;


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<InjectionArtifactComponent, ArtifactActivatedEvent>(OnActivated);

        _injectableQuery = GetEntityQuery<InjectableSolutionComponent>();

    }

    private void OnActivated(EntityUid uid, InjectionArtifactComponent component, ArtifactActivatedEvent args)
    {
        //We get all the entity in the range into which the reagent will be injected.
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(uid);
        var ents = _lookup.GetEntitiesInRange<InjectableSolutionComponent>(_transform.GetMapCoordinates(uid, xform: xform), component.Range)
            .Select(x => x.Owner).ToList();
        if (args.Activator != null)
            ents.Add(args.Activator.Value);

        //Add all chemicals to the solution
        foreach (var chem in component.Entries)
        {
            component.ChemicalSolution.AddReagent(chem.Chemical, chem.Amount);
        }

        //Try to add all the chems into each nearby entity that can take them
        foreach (var ent in ents)
        {
            if (!HasComp<InjectableSolutionComponent>(ent))
                continue;

            if (!_solutionContainer.TryGetInjectableSolution(ent, out var injectable, out _))
                continue;

            if (_injectableQuery.TryGetComponent(ent, out var injEnt))
            {
                //inject
                _solutionContainer.AddSolution(injectable.Value, component.ChemicalSolution);

                //Spawn Effect
                if (component.ShowEffect)
                {
                    var uidXform = Transform(ent);
                    Spawn(component.VisualEffectPrototype, uidXform.Coordinates);
                }
            }
        }
    }
}
