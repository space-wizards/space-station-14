using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.RetractableItemAction;

/// <summary>
/// System for handling retractable items, such as armblades.
/// </summary>
public partial class RetractableItemActionSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RetractableItemActionComponent, MapInitEvent>(OnActionInit);
        SubscribeLocalEvent<RetractableItemActionComponent, OnRetractableItemActionEvent>(OnRetractableItemAction);

        SubscribeLocalEvent<ActionRetractableItemComponent, ComponentShutdown>(OnActionSummonedShutdown);
    }

    private void OnActionInit(Entity<RetractableItemActionComponent> ent, ref MapInitEvent args)
    {
        _containers.EnsureContainer<Container>(ent, RetractableItemActionComponent.Container);

        PopulateActionItem(ent.Owner);
    }

    private void OnRetractableItemAction(Entity<RetractableItemActionComponent> ent, ref OnRetractableItemActionEvent args)
    {
        if (_hands.GetActiveHand(args.Performer) is not { } userHand)
            return;

        if (_actions.GetAction(ent.Owner) is not { } action)
            return;

        if (action.Comp.AttachedEntity == null)
            return;

        if (_hands.IsHolding(args.Performer, ent.Comp.ActionItemUid))
        {
            RemComp<UnremoveableComponent>(ent.Comp.ActionItemUid);
            var container = _containers.GetContainer(ent, RetractableItemActionComponent.Container);
            _containers.Insert(ent.Comp.ActionItemUid, container);
            _audio.PlayPredicted(ent.Comp.RetractSounds, action.Comp.AttachedEntity.Value, action.Comp.AttachedEntity.Value);
        }
        else
        {
            _hands.TryForcePickup(args.Performer, ent.Comp.ActionItemUid, userHand, checkActionBlocker: false);
            _audio.PlayPredicted(ent.Comp.SpawnSounds, action.Comp.AttachedEntity.Value, action.Comp.AttachedEntity.Value);

            // Mispredicts allowing you to drop for a very brief moment, however without it it throws a ResetPredictedEntities exception.
            // I have no idea what causes it or how to fix it.
            // My only guess is that prediction doesn't like moving an item that becomes unremovable in the same frame.
            if (_net.IsServer)
                EnsureComp<UnremoveableComponent>(ent.Comp.ActionItemUid);
        }

        args.Handled = true;
    }

    private void OnActionSummonedShutdown(Entity<ActionRetractableItemComponent> ent, ref ComponentShutdown args)
    {
        if (_actions.GetAction(ent.Comp.SummoningAction) is not { } action)
            return;

        if (!TryComp<RetractableItemActionComponent>(action, out var retract) || retract.ActionItemUid != ent.Owner)
            return;

        // If the item is somehow destroyed, re-add it to the action.
        PopulateActionItem(action.Owner);
    }

    private void PopulateActionItem(Entity<RetractableItemActionComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false) || TerminatingOrDeleted(ent))
            return;

        if (!PredictedTrySpawnInContainer(ent.Comp.SpawnedPrototype, ent.Owner, RetractableItemActionComponent.Container, out var summoned))
            return;

        ent.Comp.ActionItemUid = summoned.Value;

        // Mark the unremovable item so it can be added back into the action.
        var summonedComp = AddComp<ActionRetractableItemComponent>(summoned.Value);

        summonedComp.SummoningAction = ent.Owner;

        Dirty(summoned.Value, summonedComp);

        Dirty(ent);
    }
}
