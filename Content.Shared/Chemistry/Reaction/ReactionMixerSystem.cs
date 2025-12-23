using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Popups;

namespace Content.Shared.Chemistry.Reaction;

public sealed partial class ReactionMixerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReactionMixerComponent, AfterInteractEvent>(OnAfterInteract, before: [typeof(IngestionSystem)]);
        SubscribeLocalEvent<ReactionMixerComponent, ShakeEvent>(OnShake);
        SubscribeLocalEvent<ReactionMixerComponent, ReactionMixDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(Entity<ReactionMixerComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.Target.HasValue || !args.CanReach || !ent.Comp.MixOnInteract)
            return;

        if (!CanMix(ent.AsNullable(), args.Target.Value))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.TimeToMix, new ReactionMixDoAfterEvent(), ent, args.Target.Value, ent);

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDoAfter(Entity<ReactionMixerComponent> ent, ref ReactionMixDoAfterEvent args)
    {
        if (args.Target == null)
            return;

        if (!TryMix(ent.AsNullable(), args.Target.Value))
            return;

        _popup.PopupClient(
            Loc.GetString(
                ent.Comp.MixMessage,
                ("mixed", Identity.Entity(args.Target.Value, EntityManager)),
                ("mixer", Identity.Entity(ent.Owner, EntityManager))),
            args.User,
            args.User);
    }

    private void OnShake(Entity<ReactionMixerComponent> ent, ref ShakeEvent args)
    {
        TryMix(ent.AsNullable(), ent);
    }

    /// <summary>
    /// Returns true if given reaction mixer is able to mix the solution inside the target entity, false otherwise.
    /// </summary>
    /// <param name="ent">The reaction mixer used to cause the reaction.</param>
    /// <param name="target">The target solution container with a <see cref="MixableSolutionComponent"/>.</param>
    public bool CanMix(Entity<ReactionMixerComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp, false)) // The used entity needs the component to be able to mix a solution
            return false;

        var mixAttemptEvent = new MixingAttemptEvent(ent);
        RaiseLocalEvent(ent, ref mixAttemptEvent);
        if (mixAttemptEvent.Cancelled)
            return false;

        if (!_solutionContainer.TryGetMixableSolution(target, out _, out _))
            return false;

        return true;
    }

    /// <summary>
    /// Attempts to mix the solution inside the target entity using the given reaction mixer.
    /// </summary>
    /// <param name="ent">The reaction mixer used to cause the reaction.</param>
    /// <param name="target">The target solution container with a <see cref="MixableSolutionComponent"/>.</param>
    /// <returns>If the reaction mixer was able to mix the solution. This does not necessarily mean a reaction took place.</returns>
    public bool TryMix(Entity<ReactionMixerComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        var mixAttemptEvent = new MixingAttemptEvent(ent);
        RaiseLocalEvent(ent, ref mixAttemptEvent);
        if (mixAttemptEvent.Cancelled)
            return false;

        if (!_solutionContainer.TryGetMixableSolution(target, out var solutionEnt, out _))
            return false;

        _solutionContainer.UpdateChemicals(solutionEnt.Value, true, ent.Comp);

        var afterMixingEvent = new AfterMixingEvent(ent, target);
        RaiseLocalEvent(ent, ref afterMixingEvent);

        return true;
    }
}
