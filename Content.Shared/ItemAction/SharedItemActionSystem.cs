using System.Diagnostics.CodeAnalysis;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.ItemAction;

/// <summary>
/// System for handling the ItemRecall ability for wizards.
/// </summary>
public partial class SharedItemActionSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemActionComponent, MapInitEvent>(OnActionInit);
        SubscribeLocalEvent<ItemActionComponent, OnItemActionEvent>(OnItemAction);

        SubscribeLocalEvent<ActionSummonedItemComponent, ComponentShutdown>(OnActionSummonedShutdown);
    }

    private void OnActionInit(Entity<ItemActionComponent> ent, ref MapInitEvent args)
    {
        _containers.EnsureContainer<Container>(ent, ItemActionComponent.Container);

        PopulateActionItem(ent.Owner, out _);
    }

    private void OnItemAction(Entity<ItemActionComponent> ent, ref OnItemActionEvent args)
    {
        if (_hands.GetActiveHand(args.Performer) is not {} userHand)
            return;

        if (_actions.GetAction(ent.Owner) is not { } action)
            return;

        if (action.Comp.AttachedEntity == null)
            return;

        if (!ent.Comp.Unremovable)
        {
            PopulateActionItem(ent.Owner, out var spawned);

            if (spawned == null)
                return;

            _hands.TryForcePickup(args.Performer, spawned.Value, userHand, checkActionBlocker: false);
            args.Handled = true;
            return;
        }

        if (ent.Comp.Unremovable && ent.Comp.ActionItemUid != null)
        {
            if (ent.Comp.Summoned)
            {
                RemComp<UnremoveableComponent>(ent.Comp.ActionItemUid.Value);
                var container = _containers.GetContainer(ent, ItemActionComponent.Container);
                _containers.Insert(ent.Comp.ActionItemUid.Value, container);
                _audio.PlayPredicted(ent.Comp.RetractSounds, action.Comp.AttachedEntity.Value, action.Comp.AttachedEntity.Value);
                ent.Comp.Summoned = false;
                Dirty(ent);

                args.Handled = true;
            }
            else
            {
                _hands.TryForcePickup(args.Performer, ent.Comp.ActionItemUid.Value, userHand, checkActionBlocker: false);
                _audio.PlayPredicted(ent.Comp.SpawnSounds, action.Comp.AttachedEntity.Value, action.Comp.AttachedEntity.Value);
                EnsureComp<UnremoveableComponent>(ent.Comp.ActionItemUid.Value);
                ent.Comp.Summoned = true;
                Dirty(ent);

                args.Handled = true;
            }
        }
    }

    private void OnActionSummonedShutdown(Entity<ActionSummonedItemComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.SummoningAction is not { } action)
            return;

        // If the item is somehow destroyed, re-add it to the action.
        PopulateActionItem(action, out _);
    }

    private void PopulateActionItem(EntityUid uid, [NotNullWhen(true)] out EntityUid? item, ItemActionComponent? comp = null)
    {
        item = null;

        if (!Resolve(uid, ref comp, false) || TerminatingOrDeleted(uid))
            return;

        if (!TrySpawnInContainer(comp.SpawnedPrototype, uid, ItemActionComponent.Container, out var summoned))
            return;

        item = summoned;

        if (comp.Unremovable)
        {
            comp.ActionItemUid = summoned;

            // Mark the unremovable item so it can be added back into the action.
            var summonedComp = AddComp<ActionSummonedItemComponent>(summoned.Value);

            summonedComp.SummoningAction = uid;

            Dirty(summoned.Value, summonedComp);

            Dirty(uid, comp);
        }
    }
}
