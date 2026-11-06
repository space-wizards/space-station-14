using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Effects;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// Handles <see cref="SplashOnTriggerComponent"/>.
/// </summary>
public sealed partial class SplashOnTriggerSystem : XOnTriggerSystem<SplashOnTriggerComponent>
{
    [Dependency] private ReactiveSystem _reactive = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void OnTrigger(Entity<SplashOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.Solution, out var solutionEnt, out var solution)
            && solution.Volume > 0)
        {
            var splashed = _solutionContainer.SplitSolution(solutionEnt.Value, solution.Volume);
            _reactive.DoEntityReaction(target, splashed, ReactionMethod.Touch);

            _popup.PopupPredicted(Loc.GetString("splash-on-trigger-splashed",
                    ("entity", ent.Owner),
                    ("target", Identity.Entity(target, EntityManager))),
                target,
                args.User,
                PopupType.SmallCaution);

            // Whoever caused the trigger already saw the flash if they predicted it.
            var flashFilter = Filter.Pvs(target, entityManager: EntityManager);
            if (args.User is { } user)
                flashFilter.RemoveWhereAttachedEntity(e => e == user);
            // Not a collection expression, those compile to a call the client sandbox does not allow.
            _color.RaiseEffect(splashed.GetColor(ProtoMan), new List<EntityUid> { target }, flashFilter);

            args.Handled = true;
        }

        _audio.PlayPredicted(ent.Comp.Sound, Transform(ent).Coordinates, args.User);

        if (ent.Comp.DeleteAfterSplash)
        {
            QueueDel(ent.Owner);
            args.Handled = true;
        }
    }
}
