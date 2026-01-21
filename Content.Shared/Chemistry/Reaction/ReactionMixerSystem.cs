using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.Chemistry.Reaction;

public sealed partial class ReactionMixerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReactionMixerComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ReactionMixerComponent, AfterInteractEvent>(OnAfterInteract, before: [typeof(IngestionSystem)]);
        SubscribeLocalEvent<ReactionMixerComponent, ShakeEvent>(OnShake);
        SubscribeLocalEvent<ReactionMixerComponent, ReactionMixDoAfterEvent>(OnDoAfter);
    }

    private void OnUseInHand(Entity<ReactionMixerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.MixerType != ReactionMixerType.Handheld)
            return;

        args.Handled = true;

        if (!CanMix(ent.AsNullable(), ent))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.TimeToMix,
            new ReactionMixDoAfterEvent(),
            ent,
            ent,
            ent)
        {
            NeedHand = true,
            BreakOnDamage = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            BreakOnMove = true
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
            ent.Comp.AudioStream = _audio.PlayPredicted(ent.Comp.MixingSound, ent, args.User)?.Entity ?? ent.Comp.AudioStream;
    }

    private void OnAfterInteract(Entity<ReactionMixerComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.Target.HasValue || !args.CanReach || ent.Comp.MixerType != ReactionMixerType.Machine)
            return;

        if (!CanMix(ent.AsNullable(), args.Target.Value))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.TimeToMix, new ReactionMixDoAfterEvent(), ent, args.Target.Value, ent);

        if (_doAfter.TryStartDoAfter(doAfterArgs))
            ent.Comp.AudioStream = _audio.PlayPredicted(ent.Comp.MixingSound, ent, args.User)?.Entity ?? ent.Comp.AudioStream;

        args.Handled = true;
    }

    private void OnDoAfter(Entity<ReactionMixerComponent> ent, ref ReactionMixDoAfterEvent args)
    {
        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);

        if (args.Cancelled)
            return;

        if (args.Target == null)
            return;

        if (!TryMix(ent.AsNullable(), args.Target.Value))
            return;

        _popup.PopupClient(
            Loc.GetString(ent.Comp.MixMessage,
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

        if (!_solutionContainer.TryGetMixableSolution(target, out _, out var mixableSolution))
            return false;

        // Can't mix nothing.
        if (mixableSolution.Volume <= 0)
            return false;

        var mixAttemptEvent = new MixingAttemptEvent(ent);
        RaiseLocalEvent(ent, ref mixAttemptEvent);
        if (mixAttemptEvent.Cancelled)
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
