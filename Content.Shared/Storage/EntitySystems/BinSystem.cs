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
using Robust.Shared.Network;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// This handles <see cref="BinComponent"/>
/// </summary>
public sealed class BinSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _admin = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BinComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BinComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BinComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<BinComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<BinComponent, InteractHandEvent>(OnInteractHand, before: new[] { typeof(SharedItemSystem) });
        SubscribeLocalEvent<BinComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<BinComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
        SubscribeLocalEvent<BinComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<BinComponent> entity, ref ExaminedEvent args)
    {
        args.PushText(Loc.GetString("bin-component-on-examine-text", ("count", entity.Comp.Items.Count)));
    }

    private void OnStartup(Entity<BinComponent> entity, ref ComponentStartup args)
    {
        entity.Comp.ItemContainer = _container.EnsureContainer<Container>(entity, entity.Comp.ContainerId);
    }

    private void OnMapInit(Entity<BinComponent> entity, ref MapInitEvent args)
    {
        // don't spawn on the client.
        if (_net.IsClient)
            return;

        var xform = Transform(entity);
        foreach (var id in entity.Comp.InitialContents)
        {
            var ent = Spawn(id, xform.Coordinates);
            if (TryInsertIntoBin(entity, ent))
                continue;

            Log.Error($"Entity {ToPrettyString(ent)} was unable to be initialized into bin {ToPrettyString(entity)}");
            return;
        }
    }

    private void OnEntInserted(Entity<BinComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        ent.Comp.Items.Add(args.Entity);
    }

    private void OnEntRemoved(Entity<BinComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        ent.Comp.Items.Remove(args.Entity);
    }

    private void OnInteractHand(Entity<BinComponent> entity, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryRemoveFromBin(entity, out var toGrab))
            return;

        _hands.TryPickupAnyHand(args.User, toGrab.Value);
        args.Handled = true;
    }

    private void OnGetAltVerbs(Entity<BinComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        if (args.Using == null)
        {
            var subject = entity.Comp.Items.LastOrDefault();
            AlternativeVerb verb = new()
            {

                Act = () =>
                {
                    if (TryRemoveFromBin(entity, out var toGrab, user))
                        _hands.TryPickupAnyHand(user, toGrab.Value);
                },
                Icon = entity.Comp.RemoveIcon,
                Text = Loc.GetString("take-item-verb-text", ("subject", subject)),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }
        else if (CanInsertIntoBin(entity, args.Using.Value))
        {
            var used = args.Using.Value;
            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    TryInsertIntoBin(entity, used, user);
                },
                Icon = entity.Comp.InsertIcon,
                Text = Loc.GetString("place-item-verb-text", ("subject", used)),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnAfterInteractUsing(Entity<BinComponent> entity, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (TryInsertIntoBin(entity, args.Used, args.User))
            args.Handled = true;
    }

    public bool CanInsertIntoBin(Entity<BinComponent> entity, EntityUid toInsert)
    {
        if (entity.Comp.Items.Count >= entity.Comp.MaxItems)
            return false;

        return !_whitelistSystem.IsWhitelistFail(entity.Comp.Whitelist, toInsert);
    }

    /// <summary>
    /// Inserts an entity at the top of the bin
    /// </summary>
    /// <param name="entity">Bin entity we're trying to insert into</param>
    /// <param name="toInsert">Entity we're trying to insert</param>
    /// <param name="user">Entity who is inserting into the bin if one exists.</param>
    /// <returns>Returns true if insertion was successful.</returns>
    public bool TryInsertIntoBin(Entity<BinComponent> entity, EntityUid toInsert, EntityUid? user = null)
    {
        if (!CanInsertIntoBin(entity, toInsert))
            return false;

        _container.Insert(toInsert, entity.Comp.ItemContainer);
        Dirty(entity);

        if (user != null)
            _admin.Add(LogType.Pickup, LogImpact.Low, $"{ToPrettyString(user):player} inserted {ToPrettyString(toInsert)} into bin {ToPrettyString(entity)}.");

        return true;
    }

    /// <summary>
    /// Tries to remove an entity from the top of the bin.
    /// </summary>
    /// <param name="entity">Entity we're removing an item from.</param>
    /// <param name="removed">Item we removed.</param>
    /// <param name="user">Entity that is trying to remove from the bin.</param>
    /// <returns>Returns false if removal was successful.</returns>
    public bool TryRemoveFromBin(Entity<BinComponent> entity, [NotNullWhen(true)]out EntityUid? removed, EntityUid? user = null)
    {
        removed = null;
        if (entity.Comp.Items.Count == 0)
            return false;

        var remove = entity.Comp.Items.LastOrDefault();
        if (!_container.Remove(remove, entity.Comp.ItemContainer))
            return false;

        removed = remove;
        Dirty(entity);

        if (user != null)
            _admin.Add(LogType.Pickup, LogImpact.Low, $"{ToPrettyString(user):player} removed {ToPrettyString(remove)} from bin {ToPrettyString(entity)}.");
        return true;
    }
}
