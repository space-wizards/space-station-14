using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Glue;

public sealed partial class GlueSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private NameModifierSystem _nameMod = default!;
    [Dependency] private OpenableSystem _openable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GlueComponent, AfterInteractEvent>(OnInteract, after: new[] { typeof(OpenableSystem) });
    }

    // When glue bottle is used on item it will apply the glued and unremoveable components.
    // TODO: Move to [SubscribeLocalEvent] once "after:" can be applied.
    private void OnInteract(Entity<GlueComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (TryGlue(entity, target, args.User))
            args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnUtilityVerb(Entity<GlueComponent> entity, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Target is not { Valid: true } target ||
        _openable.IsClosed(entity))
            return;

        var user = args.User;

        var verb = new UtilityVerb()
        {
            Act = () => TryGlue(entity, target, user),
            IconEntity = GetNetEntity(entity),
            Text = Loc.GetString("glue-verb-text"),
            Message = Loc.GetString("glue-verb-message"),
        };

        args.Verbs.Add(verb);
    }

    [SubscribeLocalEvent]
    private void OnGluedInit(Entity<GluedComponent> entity, ref ComponentInit args)
    {
        _nameMod.RefreshNameModifiers(entity.Owner);
    }

    [SubscribeLocalEvent]
    private void OnHandPickUp(Entity<GluedComponent> entity, ref GotEquippedHandEvent args)
    {
        PerformGluedEffect(entity, args.User);
    }

    [SubscribeLocalEvent]
    private void OnRefreshNameModifiers(Entity<GluedComponent> entity, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("glued-name-prefix");
    }

    [SubscribeLocalEvent]
    private void OnGluedEffectAttemptEvent(Entity<GluedImmuneComponent> entity, ref GluedEffectAttemptEvent args)
    {
        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnGluedEffectAttemptEvent(Entity<GluedImmuneComponent> entity, ref InventoryRelayedEvent<GluedEffectAttemptEvent> args)
    {
        OnGluedEffectAttemptEvent(entity, ref args.Args);
    }

    private bool TryGlue(Entity<GlueComponent> entity, EntityUid target, EntityUid actor)
    {
        // if item is glued then don't apply glue again so it can be removed for reasonable time
        // If glue is applied to an unremoveable item, the component will disappear after the duration.
        // This effectively means any unremoveable item could be removed with a bottle of glue.
        if (HasComp<GluedComponent>(target) || !HasComp<ItemComponent>(target) || HasComp<UnremoveableComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("glue-failure", ("target", target)), actor, actor, PopupType.Medium);
            return false;
        }

        if (HasComp<ItemComponent>(target) && _solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out var solutionEntity, out _))
        {
            var quantity = _solutionContainer.RemoveReagent(solutionEntity.Value, entity.Comp.Reagent, entity.Comp.ConsumptionUnit);
            if (quantity > 0)
            {
                _audio.PlayPredicted(entity.Comp.Squeeze, entity.Owner, actor);
                _popup.PopupClient(Loc.GetString("glue-success", ("target", target)), actor, actor, PopupType.Medium);
                _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(actor):actor} glued {ToPrettyString(target):subject} with {ToPrettyString(entity.Owner):tool}");
                var gluedComp = EnsureComp<GluedComponent>(target);
                gluedComp.Duration = quantity.Double() * entity.Comp.DurationPerUnit;
                if (_container.TryGetContainingContainer((target, null, null), out var container) && _hands.IsHolding(container.Owner, target))
                    PerformGluedEffect((target, gluedComp), actor);

                Dirty(target, gluedComp);
                return true;
            }
        }

        _popup.PopupClient(Loc.GetString("glue-failure", ("target", target)), actor, actor, PopupType.Medium);
        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GluedComponent, UnremoveableComponent>();
        while (query.MoveNext(out var uid, out var glue, out var _))
        {
            if (_timing.CurTime < glue.Until)
                continue;

            RemComp<UnremoveableComponent>(uid);
            RemComp<GluedComponent>(uid);

            _nameMod.RefreshNameModifiers(uid);
        }
    }

    private void PerformGluedEffect(Entity<GluedComponent> entity, EntityUid user)
    {
        // Check if anything on the user cancels it.
        var attemptEv = new GluedEffectAttemptEvent();
        RaiseLocalEvent(user, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        // When predicting dropping a glued item prediction will reinsert the item into the hand when rerolling the state to a previous one.
        // So dropping the item would add UnRemoveableComponent on the client without this guard statement.
        if (_timing.ApplyingState)
            return;

        var comp = EnsureComp<UnremoveableComponent>(entity);
        comp.DeleteOnDrop = false;
        entity.Comp.Until = _timing.CurTime + entity.Comp.Duration;
        Dirty(entity.Owner, comp);
        Dirty(entity);
    }
}

/// <summary>
/// Raised on an entity to determine if it will be affected by a glued item or not.
/// </summary>
[ByRefEvent]
public record struct GluedEffectAttemptEvent() : IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.GLOVES;

    /// <summary>
    /// If true, prevents the effect from being applied.
    /// </summary>
    public bool Cancelled;
}
