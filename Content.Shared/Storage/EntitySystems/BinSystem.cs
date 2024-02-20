using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// This handles <see cref="BinComponent"/>
/// </summary>
public sealed class BinSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _admin = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public const string BinContainerId = "bin-container";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BinComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BinComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BinComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<BinComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<BinComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<BinComponent, GetVerbsEvent<AlternativeVerb>>(OnAltInteractHand);
        SubscribeLocalEvent<BinComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, BinComponent component, ExaminedEvent args)
    {
        args.PushText(Loc.GetString("bin-component-on-examine-text", ("count", component.Items.Count)));
    }

    private void OnStartup(EntityUid uid, BinComponent component, ComponentStartup args)
    {
        component.ItemContainer = _container.EnsureContainer<Container>(uid, BinContainerId);
    }

    private void OnMapInit(EntityUid uid, BinComponent component, MapInitEvent args)
    {
        // don't spawn on the client.
        if (_net.IsClient)
            return;

        var xform = Transform(uid);
        foreach (var id in component.InitialContents)
        {
            var ent = Spawn(id, xform.Coordinates);
            if (!TryInsertIntoBin(uid, ent, component))
            {
                Log.Error($"Entity {ToPrettyString(ent)} was unable to be initialized into bin {ToPrettyString(uid)}");
                return;
            }
        }
    }

    private void OnEntRemoved(EntityUid uid, BinComponent component, EntRemovedFromContainerMessage args)
    {
        component.Items.Remove(args.Entity);
    }

    private void OnInteractHand(EntityUid uid, BinComponent component, InteractHandEvent args)
    {
        if (args.Handled || !_timing.IsFirstTimePredicted)
            return;

        EntityUid? toGrab = component.Items.LastOrDefault();
        if (!TryRemoveFromBin(uid, toGrab, component))
            return;

        _hands.TryPickupAnyHand(args.User, toGrab.Value);
        _admin.Add(LogType.Pickup, LogImpact.Low,
            $"{ToPrettyString(uid):player} removed {ToPrettyString(toGrab.Value)} from bin {ToPrettyString(uid)}.");
        args.Handled = true;
    }

    /// <summary>
    /// Alt interact acts the same as interacting with your hands normally, but allows fallback interaction if the item
    /// has priority. E.g. a water cup on a water cooler fills itself on a normal click,
    /// but you can use alternative interactions to restock the cup bin
    /// </summary>
    private void OnAltInteractHand(EntityUid uid, BinComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.Using != null)
        {
            var canReach = args.CanAccess && args.CanInteract;
            InsertIntoBin(args.User, args.Target, (EntityUid) args.Using, component, false, canReach);
        }
    }

    private void OnAfterInteractUsing(EntityUid uid, BinComponent component, AfterInteractUsingEvent args)
    {
        InsertIntoBin(args.User, uid, args.Used, component, args.Handled, args.CanReach);
        args.Handled = true;
    }

    private void InsertIntoBin(EntityUid user, EntityUid target, EntityUid itemInHand, BinComponent component, bool handled, bool canReach)
    {
        if (handled || !canReach)
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryInsertIntoBin(target, itemInHand, component))
            return;

        _admin.Add(LogType.Pickup, LogImpact.Low, $"{ToPrettyString(target):player} inserted {ToPrettyString(user)} into bin {ToPrettyString(target)}.");
    }

    /// <summary>
    /// Inserts an entity at the top of the bin
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="toInsert"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public bool TryInsertIntoBin(EntityUid uid, EntityUid toInsert, BinComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.Items.Count >= component.MaxItems)
            return false;

        if (component.Whitelist != null && !component.Whitelist.IsValid(toInsert))
            return false;

        _container.Insert(toInsert, component.ItemContainer);
        component.Items.Add(toInsert);
        Dirty(component);
        return true;
    }

    /// <summary>
    /// Tries to remove an entity from the top of the bin.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="toRemove"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public bool TryRemoveFromBin(EntityUid uid, EntityUid? toRemove, BinComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!component.Items.Any())
            return false;

        if (toRemove == null || toRemove != component.Items.LastOrDefault())
            return false;

        if (!_container.Remove(toRemove.Value, component.ItemContainer))
            return false;

        component.Items.Remove(toRemove.Value);
        Dirty(component);
        return true;
    }
}
