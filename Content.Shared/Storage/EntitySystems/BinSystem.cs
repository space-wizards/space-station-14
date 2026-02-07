using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// This handles <see cref="BinComponent"/>
/// </summary>
public sealed class BinSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _admin = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BinComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BinComponent, InteractHandEvent>(OnInteractHand, before: new[] { typeof(SharedItemSystem) });
        SubscribeLocalEvent<BinComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<BinComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
        SubscribeLocalEvent<BinComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<BinComponent> entity, ref ExaminedEvent args)
    {
        if (entity.Comp.ItemContainer.ContainedEntities.LastOrDefault() is {Valid: true} next)
        {
            var meta = MetaData(next);
            // This means we have a contained item
            args.PushText(Loc.GetString(
                entity.Comp.ExamineText,
                ("count", entity.Comp.ItemContainer.Count),
                ("subject", meta.EntityPrototype?.Name ?? meta.EntityName)));

            return;
        }

        args.PushText(Loc.GetString(entity.Comp.EmptyText));
    }

    private void OnStartup(Entity<BinComponent> entity, ref ComponentStartup args)
    {
        entity.Comp.ItemContainer = _container.EnsureContainer<Container>(entity, entity.Comp.ContainerId);
    }

    private void OnInteractHand(Entity<BinComponent> entity, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryRemoveFromBin((entity, entity), args.User))
            return;

        args.Handled = true;
    }

    private void OnGetAltVerbs(Entity<BinComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        if (args.Using != null && CanInsertIntoBin((entity, entity), args.Using.Value))
        {
            var used = args.Using.Value;
            AlternativeVerb verb = new()
            {
                Act = () => TryInsertIntoBin((entity, entity), used, user),
                Icon = entity.Comp.InsertIcon,
                Text = Loc.GetString("place-item-verb-text", ("subject", used)),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }
        else if (entity.Comp.ItemContainer.ContainedEntities.LastOrDefault() is { Valid: true } next)
        {
            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    TryRemoveFromBin((entity, entity), user);
                },
                Icon = entity.Comp.RemoveIcon,
                Text = Loc.GetString("take-item-verb-text", ("subject", next)),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnAfterInteractUsing(Entity<BinComponent> entity, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (TryInsertIntoBin((entity, entity), args.Used, args.User))
            args.Handled = true;
    }

    public bool CanInsertIntoBin(Entity<BinComponent?> entity, EntityUid toInsert)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        if (entity.Comp.ItemContainer.Count >= entity.Comp.MaxItems)
            return false;

        return _whitelistSystem.IsWhitelistPassOrNull(entity.Comp.Whitelist, toInsert);
    }

    /// <summary>
    /// Inserts an entity at the top of the bin
    /// </summary>
    /// <param name="entity">Bin entity we're trying to insert into</param>
    /// <param name="toInsert">Entity we're trying to insert</param>
    /// <param name="user">Entity who is inserting into the bin if one exists.</param>
    /// <returns>Returns true if insertion was successful.</returns>
    public bool TryInsertIntoBin(Entity<BinComponent?> entity, EntityUid toInsert, EntityUid? user = null)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        if (!CanInsertIntoBin((entity, entity), toInsert))
            return false;

        _container.Insert(toInsert, entity.Comp.ItemContainer);

        if (user != null)
        {
            _admin.Add(LogType.Pickup,
                LogImpact.Low,
                $"{ToPrettyString(user):player} inserted {ToPrettyString(toInsert)} into bin {ToPrettyString(entity)}.");
        }

        return true;
    }

    /// <summary>
    /// Tries to remove an entity from the top of the bin, returns the removed item.
    /// </summary>
    /// <param name="entity">Entity we're removing an item from.</param>
    /// <param name="removed">Entity that was removed from the bin.</param>
    /// <returns>Returns true if removal was successful.</returns>
    public bool TryRemoveFromBin(Entity<BinComponent?> entity, [NotNullWhen(true)] out EntityUid? removed)
    {
        removed = null;
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        if (entity.Comp.ItemContainer.ContainedEntities.LastOrDefault() is not { Valid: true } toRemove)
            return false;

        if (!_container.Remove(toRemove, entity.Comp.ItemContainer))
            return false;

        removed = toRemove;
        return true;
    }

    /// <summary>
    /// Overflow of <see cref="TryRemoveFromBin(Entity{BinComponent?},out EntityUid?)"/> which takes a user, and
    /// doesn't return an item instead placing it in the user's hand or dropping it on them if their hands are full.
    /// </summary>
    /// <param name="entity">Bin the item is being removed from.</param>
    /// <param name="user">User who is picking up the item.</param>
    /// <returns>Returns true if removal was successful.</returns>
    public bool TryRemoveFromBin(Entity<BinComponent?> entity, EntityUid user)
    {
        if (!TryRemoveFromBin(entity, out var removed))
            return false;

        _admin.Add(
            LogType.Pickup,
            LogImpact.Low,
            $"{ToPrettyString(user):player} removed {ToPrettyString(removed)} from bin {ToPrettyString(entity)}.");

        _hands.PickupOrDrop(user, removed.Value, dropNear: true);

        return true;
    }
}
