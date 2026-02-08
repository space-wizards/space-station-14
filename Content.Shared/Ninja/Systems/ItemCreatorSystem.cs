using Content.Shared.Actions;
using Content.Shared.Ninja.Components;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// Handles predicting that the action exists, creating items is done serverside.
/// </summary>
public abstract class SharedItemCreatorSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemCreatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ItemCreatorComponent, GetItemActionsEvent>(OnGetActions);
    }

    private void OnMapInit(Entity<ItemCreatorComponent> ent, ref MapInitEvent args)
    {
        foreach (var entry in ent.Comp.Entries)
        {
            // test funny dont mind me
            if (string.IsNullOrEmpty(entry.Action))
                return;

            _actionContainer.EnsureAction(ent, ref entry.ActionEntity, entry.Action);
        }

        Dirty(ent);
    }

    private void OnGetActions(Entity<ItemCreatorComponent> ent, ref GetItemActionsEvent args)
    {
        if (CheckItemCreator(ent, args.User))
        {
            foreach (var entry in ent.Comp.Entries)
            {
                args.AddAction(entry.ActionEntity);
            }
        }
    }

    public bool CheckItemCreator(EntityUid uid, EntityUid user)
    {
        var ev = new CheckItemCreatorEvent(user);
        RaiseLocalEvent(uid, ref ev);
        return !ev.Cancelled;
    }
}

/// <summary>
/// Raised on the item creator before adding the action.
/// </summary>
[ByRefEvent]
public record struct CheckItemCreatorEvent(EntityUid User, bool Cancelled = false);

/// <summary>
/// Raised on the item creator before creating an item.
/// </summary>
[ByRefEvent]
public record struct CreateItemAttemptEvent(EntityUid User, bool Cancelled = false);
