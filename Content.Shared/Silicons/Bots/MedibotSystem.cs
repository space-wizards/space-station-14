using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio.Systems;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Silicons.Bots;

/// <summary>
/// Handles emagging medibots and provides api.
/// </summary>
public sealed class MedibotSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    private EntityQuery<InjectorComponent> _injectorQuery = default!;
    private EntityQuery<MobStateComponent> _mobStateQuery = default!;
    private EntityQuery<MedibotComponent> _medibotQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmaggableMedibotComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<MedibotComponent, UserActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<MedibotComponent, InjectorDoAfterEvent>(OnDoAfter);

        _injectorQuery = GetEntityQuery<InjectorComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _medibotQuery = GetEntityQuery<MedibotComponent>();
    }

    private void OnDoAfter(Entity<MedibotComponent> ent, ref InjectorDoAfterEvent args)
    {
        if (!_solutionContainer.TryGetSolution(ent.Owner, "injector", out var solution))
            return;

        // Empty the "syringe" after a doafter, to stop people from duping trico and inaprov
        _solutionContainer.RemoveAllSolution(solution.Value);
    }

    private void OnActivateInWorld(Entity<MedibotComponent> ent, ref UserActivateInWorldEvent args)
    {
        if (!TryComp(args.Target, out TransformComponent? xform))
            return;

        if(!TryGetTreatment(ent, args.Target, out var treatment))
            return;

        if (!_injectorQuery.TryComp(ent.Owner, out var injector))
            return;

        if (!_solutionContainer.TryGetSolution(ent.Owner, "injector", out var solution))
            return;

        _solutionContainer.TryAddReagent(solution.Value, treatment.Reagent, treatment.Quantity, out var quantityAdded);
        injector.ToggleState = InjectorToggleMode.Inject;

        _interactionSystem.InteractDoAfter(ent, ent, args.Target, xform.Coordinates, true);
        args.Handled = true;
    }

    private void OnEmagged(EntityUid uid, EmaggableMedibotComponent comp, ref GotEmaggedEvent args)
    {
        if (!TryComp<MedibotComponent>(uid, out var medibot))
            return;

        _audio.PlayPredicted(comp.SparkSound, uid, args.UserUid);

        foreach (var (state, treatment) in comp.Replacements)
        {
            medibot.Treatments[state] = treatment;
        }

        args.Handled = true;
    }

    public bool TryGetTreatment(EntityUid ent, EntityUid target, [NotNullWhen(true)] out MedibotTreatment? treatment)
    {
        treatment = null;

        if (!_mobStateQuery.TryComp(target, out var MobState))
            return false;

        if (!_medibotQuery.TryComp(ent, out var medibot))
            return false;

        if(!TryGetTreatment(medibot, MobState.CurrentState, out treatment))
            return false;

        return true;
    }

    /// <summary>
    /// Get a treatment for a given mob state.
    /// </summary>
    /// <remarks>
    /// This only exists because allowing other execute would allow modifying the dictionary, and Read access does not cover TryGetValue.
    /// </remarks>
    public bool TryGetTreatment(MedibotComponent comp, MobState state, [NotNullWhen(true)] out MedibotTreatment? treatment)
    {
        return comp.Treatments.TryGetValue(state, out treatment);
    }
}
