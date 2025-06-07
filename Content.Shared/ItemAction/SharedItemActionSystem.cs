using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
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

        Log.Debug("Initial setup");
        if (!ent.Comp.Unremovable)
        {
            Log.Debug("Removable population");
            PopulateActionItem(ent.Owner, out var spawned);

            if (spawned == null)
                return;

            Log.Debug("Picked up");
            _hands.TryForcePickup(args.Performer, spawned.Value, userHand, checkActionBlocker: false);
            args.Handled = true;
            return;
        }

        Log.Debug("Unremovable");
        if (ent.Comp.Unremovable && ent.Comp.ActionItemUid != null)
        {
            if (ent.Comp.Summoned)
            {
                Log.Debug("Attempting to hide");
                RemComp<UnremoveableComponent>(ent.Comp.ActionItemUid.Value);
                var container = _containers.GetContainer(ent, ItemActionComponent.Container);
                _containers.Insert(ent.Comp.ActionItemUid.Value, container);
                ent.Comp.Summoned = false;
            }
            else
            {
                Log.Debug("Attempting to pick up");
                _hands.TryForcePickup(args.Performer, ent.Comp.ActionItemUid.Value, userHand, checkActionBlocker: false);
                EnsureComp<UnremoveableComponent>(ent.Comp.ActionItemUid.Value);
                ent.Comp.Summoned = true;
            }
        }
    }

    private void OnActionSummonedShutdown(Entity<ActionSummonedItemComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.SummoningAction is not { } action)
            return;

        Log.Debug("Shutting down." + ent);

        // If the item is somehow destroyed, re-add it to the action.
        PopulateActionItem(action, out _);
    }

    private void PopulateActionItem(EntityUid uid, [NotNullWhen(true)] out EntityUid? item, ItemActionComponent? comp = null)
    {
        Log.Debug("Populating");
        item = null;

        if (!Resolve(uid, ref comp) || TerminatingOrDeleted(uid))
            return;

        Log.Debug("Spawning predicted");
        // Client crashes if unpredicted spawn is used.
        // But the client will never be able to use the item fast enough for it to cause issues anyways.
        if (!TrySpawnInContainer(comp.SpawnedPrototype, uid, ItemActionComponent.Container, out var summoned))
            return;

        item = summoned;

        Log.Debug("Checking unremovable");
        if (comp.Unremovable)
        {
            comp.ActionItemUid = summoned;

            // Mark the unremovable item so it can be added back into the action.
            var summonedComp = AddComp<ActionSummonedItemComponent>(summoned.Value);

            summonedComp.SummoningAction = uid;

            Dirty(summoned.Value, summonedComp);

            DirtyField(uid, comp, nameof(ItemActionComponent.ActionItemUid));
            Log.Debug("Summoned and dirtied and all that");
        }
    }
}
