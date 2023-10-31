using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.Stains;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Server.Stains;

public sealed class StainsSystem : SharedStainsSystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StainableComponent, SolutionChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<StainableComponent, GetVerbsEvent<AlternativeVerb>>(AddSqueezeVerb);
        SubscribeLocalEvent<StainableComponent, SqueezeDoAfterEvent>(OnSqueezeDoAfter);
    }

    /// <summary>
    /// Adds a solution to the "stains" container, if it can fully fit.
    /// </summary>
    public bool TryAddSolution(EntityUid uid, Solution addedSolution, Solution? targetSolution = null, StainableComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || targetSolution == null && !TryGetStainsSolution(uid, out targetSolution, component))
        {
            return false;
        }
        return _solutionContainer.TryAddSolution(uid, targetSolution, addedSolution);
    }

    /// <summary>
    /// Get the solution holding "stains"
    /// </summary>
    public bool TryGetStainsSolution(EntityUid uid, [NotNullWhen(true)] out Solution? solution, StainableComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
        {
            solution = null;
            return false;
        }
        return _solutionContainer.TryGetSolution(uid, component.Solution, out solution);
    }

    private void OnSolutionChanged(EntityUid uid, StainableComponent component, SolutionChangedEvent @event)
    {
        if (@event.Solution.Name == component.Solution)
        {
            var color = @event.Solution.GetColor(_prototype);
            var lambda = @event.Solution.FillFraction / 4;
            component.StainColor = Color.InterpolateBetween(Color.White, color, lambda);
            Dirty(uid, component);
        }
    }

    private void AddSqueezeVerb(EntityUid uid, StainableComponent component, GetVerbsEvent<AlternativeVerb> @event)
    {
        if (!@event.CanAccess || !@event.CanInteract)
        {
            return;
        }

        if (!_solutionContainer.TryGetSolution(uid, component.Solution, out var solution))
        {
            return;
        }

        if (solution.Volume <= 0)
        {
            return;
        }

        @event.Verbs.Add(new AlternativeVerb
        {
            Act = () => Squeeze(uid, @event.User, component),
            Text = Loc.GetString("comp-stainable-verb-squeeze")
        });
    }

    private void Squeeze(EntityUid uid, EntityUid user, StainableComponent component)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, user, component.SqueezeDuration, new SqueezeDoAfterEvent(), uid, uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnSqueezeDoAfter(EntityUid uid, StainableComponent component, SqueezeDoAfterEvent @event)
    {
        if (@event.Cancelled)
            return;

        if (!_solutionContainer.TryGetSolution(uid, component.Solution, out var solution))
        {
            return;
        }

        _puddle.TrySpillAt(uid, solution, out _, sound: false);
        _solutionContainer.RemoveAllSolution(uid, solution);
        _audio.PlayPvs(component.SqueezeSound, uid);
    }
}
