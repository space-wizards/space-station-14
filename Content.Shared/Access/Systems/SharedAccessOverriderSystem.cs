using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Access.Systems;

public abstract partial class SharedAccessOverriderSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccessOverriderComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<AccessOverriderComponent, ComponentRemove>(OnComponentRemove);

        SubscribeLocalEvent<AccessOverriderComponent, EntInsertedIntoContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<AccessOverriderComponent, EntRemovedFromContainerMessage>(UpdateUserInterface);

        SubscribeLocalEvent<AccessOverriderComponent, GetVerbsEvent<UtilityVerb>>(OnGetVerbs);
        SubscribeLocalEvent<AccessOverriderComponent, AfterInteractEvent>(AfterInteractOn);
        SubscribeLocalEvent<AccessOverriderComponent, AccessOverriderDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);

        Subs.BuiEvents<AccessOverriderComponent>(AccessOverriderUiKey.Key,
            subs =>
            {
                // It kinda drives me nuts that I need to subscribe to the BUI
                // open but if you don't you get so many prediction issues due
                // to ordering of BUI opens versus updates and I lost so many
                // hours of my life trying to fix those rather than just
                // subscribing.
                subs.Event<BoundUIOpenedEvent>(UpdateUserInterface);
                subs.Event<BoundUIClosedEvent>(OnClose);
                subs.Event<SetAccessesMessage>(OnSetAccessesMessage);
            });
    }

    private void OnComponentInit(EntityUid uid, AccessOverriderComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, AccessOverriderComponent.PrivilegedIdCardSlotId, component.PrivilegedIdSlot);
    }

    private void OnComponentRemove(EntityUid uid, AccessOverriderComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.PrivilegedIdSlot);
    }

    private void UpdateUserInterface<T>(Entity<AccessOverriderComponent> ent, ref T args)
    {
        DirtyUI(ent);
    }

    protected virtual void DirtyUI(EntityUid uid) { }

    private void OnGetVerbs(Entity<AccessOverriderComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        var user = args.User;
        var target = args.Target;

        if (!CanConfigurate(user, target))
            return;

        args.Verbs.Add(new UtilityVerb
        {
            Act = () => StartDoAfter(ent, user, target),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png")),
            Text = Loc.GetString("access-overrider-verb-modify-access"),
        });
    }

    private void AfterInteractOn(Entity<AccessOverriderComponent> ent, ref AfterInteractEvent args)
    {
        if (!CanConfigurate(args.User, args.Target))
            return;

        StartDoAfter(ent, args.User, args.Target.Value);
    }

    private bool CanConfigurate(EntityUid user, [NotNullWhen(true)] EntityUid? target)
    {
        return target is not null
               && HasComp<AccessReaderComponent>(target)
               && _interactionSystem.InRangeUnobstructed(user, target.Value);
    }

    private void StartDoAfter(Entity<AccessOverriderComponent> ent, EntityUid user, EntityUid target)
    {
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            ent.Comp.DoAfterDuration,
            new AccessOverriderDoAfterEvent(),
            ent,
            target: target,
            used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        });
    }

    private void OnDoAfter(Entity<AccessOverriderComponent> ent, ref AccessOverriderDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !CanConfigurate(args.User, args.Target))
            return;

        args.Handled = true;

        ent.Comp.TargetAccessReaderId = args.Target;
        Dirty(ent);
        UI.OpenUi(ent.Owner, AccessOverriderUiKey.Key, args.User);
    }

    private void OnClose(Entity<AccessOverriderComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.TargetAccessReaderId = null;
        Dirty(ent);
    }

    private void OnSetAccessesMessage(Entity<AccessOverriderComponent> ent, ref SetAccessesMessage args)
    {
        if (ent.Comp.TargetAccessReaderId is not { } targetAccessReaderId
            || ent.Comp.PrivilegedIdSlot.Item is not { } idCard)
            return;

        if (!PrivilegedIdIsAuthorized(ent.AsNullable()))
            return;

        if (!_interactionSystem.InRangeUnobstructed(args.Actor, targetAccessReaderId))
        {
            _popupSystem.PopupClient(Loc.GetString("access-overrider-out-of-range"), args.Actor, args.Actor);
            return;
        }

        if (args.AccessList.Count > 0 && !args.AccessList.All(x => ent.Comp.AccessLevels.Contains(x)))
        {
            Log.Warning($"User {ToPrettyString(args.Actor)} tried to write unknown access tag.");
            return;
        }

        if (!_accessReader.GetMainAccessReader(targetAccessReaderId, out var accessReaderEnt)
            || accessReaderEnt is not { } mainReader)
            return;

        var oldTags = accessReaderEnt.Value.Comp.AccessLists.SelectMany(x => x).ToList();

        if (oldTags.SequenceEqual(args.AccessList))
            return;

        var difference = args.AccessList.Union(oldTags).Except(args.AccessList.Intersect(oldTags)).ToHashSet();
        var privilegedPerms = _accessReader.FindAccessTags(idCard).ToHashSet();

        if (!difference.IsSubsetOf(privilegedPerms))
        {
            Log.Warning($"User {ToPrettyString(args.Actor)} tried to modify permissions they could not give/take!");
            return;
        }

        if (!oldTags.ToHashSet().IsSubsetOf(privilegedPerms))
        {
            Log.Warning($"User {ToPrettyString(args.Actor)} tried to modify permissions when they do not have "
                        + $"sufficient access!");
            return;
        }

        var addedTags = args.AccessList.Except(oldTags).Select(tag => "+" + tag).ToList();
        var removedTags = oldTags.Except(args.AccessList).Select(tag => "-" + tag).ToList();

        _adminLogger.Add(LogType.Action,
            LogImpact.High,
            $"{ToPrettyString(args.Actor):player} has modified "
            + $"{ToPrettyString(mainReader):entity} with the following allowed access level holders: "
            + $"[{string.Join(", ", addedTags.Union(removedTags))}] [{string.Join(", ", args.AccessList)}]");

        _accessReader.TrySetAccesses(mainReader, args.AccessList);

        var ev = new OnAccessOverriderAccessUpdatedEvent(args.Actor);
        RaiseLocalEvent(targetAccessReaderId, ref ev);

        DirtyUI(ent);
    }

    /// <summary>
    /// Returns true if there is an ID in
    /// <see cref="AccessOverriderComponent.PrivilegedIdSlot"/> and said ID
    /// satisfies the requirements of <see cref="AccessReaderComponent"/>.
    /// </summary>
    /// <remarks>
    /// Other code relies on the fact this returns false if privileged Id is
    /// null. Don't break that invariant.
    /// </remarks>
    public bool PrivilegedIdIsAuthorized(Entity<AccessOverriderComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return true;

        if (_accessReader.GetMainAccessReader(ent, out var accessReader))
            return true;

        return ent.Comp.PrivilegedIdSlot.Item is { } item
               && _accessReader.IsAllowed(item, ent, accessReader);
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs ev)
    {
        if (!ev.WasModified<EntityPrototype>())
            return;

        var query = AllEntityQuery<AccessOverriderComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (MetaData(uid).EntityPrototype is not { } proto
                || !proto.TryGetComponent<AccessOverriderComponent>(out var protoComp, Factory))
                continue;

            // Will be true in nearly all cases so don't dirty a load of
            // stuff for no reason.
            if (comp.AccessLevels.Order().SequenceEqual(protoComp.AccessLevels.Order()))
                continue;

            // We take the union rather than replacing because we don't want
            // to remove accesses from a configurator that had some added
            // via VV.
            comp.AccessLevels = comp.AccessLevels.Union(protoComp.AccessLevels).ToList();
            Dirty(uid, comp);
        }
    }
}

/// <summary>
/// Raised against an access reader when its accesses are changed by an access
/// configurator (AKA overrider).
/// </summary>
/// <param name="UserUid">The user who changed the door's accesses.</param>
[ByRefEvent]
public record struct OnAccessOverriderAccessUpdatedEvent(EntityUid UserUid);

/// <summary>
/// Doafter event raised from using an access overrider on an entity.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class AccessOverriderDoAfterEvent : SimpleDoAfterEvent;
