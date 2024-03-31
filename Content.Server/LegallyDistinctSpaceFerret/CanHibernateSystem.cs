using Content.Server.Actions;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Popups;
using Content.Shared.Interaction.Components;
using Content.Shared.LegallyDistinctSpaceFerret;
using Content.Shared.Mind;
using Content.Shared.NPC;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Server.LegallyDistinctSpaceFerret;

public sealed class CanHibernateSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanHibernateComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<CanHibernateComponent, EepyActionEvent>(OnEepyAction);
    }

    private void OnInit(EntityUid uid, CanHibernateComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.EepyActionEntity, component.EepyAction, uid);
    }

    public void OnEepyAction(EntityUid uid, CanHibernateComponent comp, EepyActionEvent args)
    {
        // If you require food before hibernating, assert that this condition has been fulfilled.
        if (_mind.TryGetObjectiveComp<ConsumeNutrientsConditionComponent>(uid, out var nutrientsCondition) && nutrientsCondition.NutrientsConsumed / nutrientsCondition.NutrientsRequired < 1.0)
        {
            _popup.PopupEntity(Loc.GetString(comp.NotEnoughNutrientsMessage), uid, PopupType.SmallCaution);

            return;
        }

        // Assert that you're near a hibernation point (scrubbers)
        var scrubbers = _lookup.GetEntitiesInRange<GasVentScrubberComponent>(Transform(uid).Coordinates, 2f);
        if (scrubbers.Count <= 0)
        {
            _popup.PopupEntity(Loc.GetString(comp.TooFarFromHibernationSpot), uid, PopupType.SmallCaution);

            return;
        }

        if (_mind.TryGetObjectiveComp<HibernateConditionComponent>(uid, out var hibernateCondition))
        {
            _audio.PlayPvs(new SoundPathSpecifier(hibernateCondition.SuccessSfx), uid);
            _popup.PopupEntity(Loc.GetString(hibernateCondition.SuccessMessage), uid, PopupType.Large);
            hibernateCondition.Hibernated = true;
        }

        // Kick player out
        var mind = _mind.GetMind(uid);
        if (mind != null)
        {
            _ticker.OnGhostAttempt(mind.Value, false);
        }

        // End ghost-role
        AddComp<BlockMovementComponent>(uid);
        RemComp<ActiveNPCComponent>(uid);
        RemComp<GhostTakeoverAvailableComponent>(uid);

        // Notify
        RaiseNetworkEvent(new EntityHasHibernated(GetNetEntity(uid), comp.SpriteStateId));

        args.Handled = true;
    }
}

public struct EntityHibernateAttemptSuccessEvent(EntityUid entity)
{
    public EntityUid Entity = entity;
}
