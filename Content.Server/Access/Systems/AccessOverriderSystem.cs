using Content.Server.AirlockPainter;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Content.Shared.StatusIcon;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.Linq;
using TerraFX.Interop.Windows;
using static Content.Shared.Access.Components.AccessOverriderComponent;
using Content.Server.Popups;
using static Content.Shared.Administration.Logs.AdminLogsEuiMsg;

namespace Content.Server.Access.Systems;

[UsedImplicitly]
public sealed class AccessOverriderSystem : SharedAccessOverriderSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly StationRecordsSystem _record = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly AccessSystem _access = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccessOverriderComponent, WriteToTargetAccessReaderIdMessage>(OnWriteToTargetAccessReaderIdMessage);
        SubscribeLocalEvent<AccessOverriderComponent, ComponentStartup>(UpdateUserInterface);
        SubscribeLocalEvent<AccessOverriderComponent, EntInsertedIntoContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<AccessOverriderComponent, EntRemovedFromContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<AccessOverriderComponent, AfterInteractEvent>(AfterInteractOn);
    }

    private void AfterInteractOn(EntityUid uid, AccessOverriderComponent component, AfterInteractEvent args)
    {
        if (!TryComp(args?.User, out ActorComponent? actor))
            return;

        if (!TryComp(args?.Target, out AccessReaderComponent? accessReader))
            return;

        component.TargetAccessReaderId = args.Target.Value;
        _userInterface.TryOpen(uid, AccessOverriderUiKey.Key, actor.PlayerSession);
    }

    private void OnWriteToTargetAccessReaderIdMessage(EntityUid uid, AccessOverriderComponent component, WriteToTargetAccessReaderIdMessage args)
    {
        if (args.Session.AttachedEntity is not { Valid: true } player)
            return;

        TryWriteToTargetAccessReaderId(uid, args.AccessList, component);

        UpdateUserInterface(uid, component, args);
    }

    private void UpdateUserInterface(EntityUid uid, AccessOverriderComponent component, EntityEventArgs args)
    {
        if (!component.Initialized)
            return;

        var privilegedIdName = string.Empty;
        string[]? possibleAccess = null;
        string[]? currentAccess = null;

        if (component.PrivilegedIdSlot.Item is { Valid: true } item)
        {
            privilegedIdName = EntityManager.GetComponent<MetaDataComponent>(item).EntityName;
            possibleAccess = _accessReader.FindAccessTags(item).ToArray();
        }

        _popupSystem.PopupEntity("test4", uid, uid);

        if (component.TargetAccessReaderId is { Valid: true })
        {
            currentAccess = _accessReader.FindAccessTags(component.TargetAccessReaderId).ToArray();
            _popupSystem.PopupEntity("test5", uid, uid);

        }

        AccessOverriderBoundUserInterfaceState newState;

        newState = new AccessOverriderBoundUserInterfaceState(
            component.PrivilegedIdSlot.HasItem,
            PrivilegedIdIsAuthorized(uid, component),
            currentAccess,
            possibleAccess,
            privilegedIdName);

        _userInterface.TrySetUiState(uid, AccessOverriderUiKey.Key, newState);
    }

    /// <summary>
    /// Called whenever an access button is pressed, adding or removing that access from the target ID card.
    /// Writes data passed from the UI into the ID stored in <see cref="AccessOverriderComponent.TargetIdSlot"/>, if present.
    /// </summary>
    private void TryWriteToTargetAccessReaderId(EntityUid uid,
        List<string> newAccessList,
        AccessOverriderComponent? component = null)
    {
        _popupSystem.PopupEntity("test9", uid, uid);

        if (!Resolve(uid, ref component) || component.TargetAccessReaderId is not { Valid: true })
            return;

        if (!PrivilegedIdIsAuthorized(uid, component))
            return;

        if (!newAccessList.TrueForAll(x => component.AccessLevels.Contains(x)))
        {
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to write unknown access tag.");
            return;
        }

        var oldTags = new List<string>();
        oldTags = oldTags.ToList();

        var privilegedId = component.PrivilegedIdSlot.Item;

        if (oldTags.SequenceEqual(newAccessList))
            return;

        var difference = newAccessList.Union(oldTags).Except(newAccessList.Intersect(oldTags)).ToHashSet();
        var privilegedPerms = _accessReader.FindAccessTags(privilegedId!.Value).ToHashSet();

        if (!difference.IsSubsetOf(privilegedPerms))
        {
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to modify permissions they could not give/take!");
            return;
        }

        var addedTags = newAccessList.Except(oldTags).Select(tag => "+" + tag).ToList();
        var removedTags = oldTags.Except(newAccessList).Select(tag => "-" + tag).ToList();

        TryComp(component.TargetAccessReaderId, out AccessReaderComponent? accessReader);

        _popupSystem.PopupEntity("test6", uid, uid);

        if (accessReader == null)
            return;

        accessReader.AccessLists = new List<HashSet<string>>() { newAccessList.ToHashSet() };
        Dirty(accessReader);

        _popupSystem.PopupEntity("test 7", uid, uid);
    }

    /// <summary>
    /// Returns true if there is an ID in <see cref="AccessOverriderComponent.PrivilegedIdSlot"/> and said ID satisfies the requirements of <see cref="AccessReaderComponent"/>.
    /// </summary>
    /// <remarks>
    /// Other code relies on the fact this returns false if privileged Id is null. Don't break that invariant.
    /// </remarks>
    private bool PrivilegedIdIsAuthorized(EntityUid uid, AccessOverriderComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return true;

        if (!EntityManager.TryGetComponent<AccessReaderComponent>(uid, out var reader))
            return true;

        var privilegedId = component.PrivilegedIdSlot.Item;
        return privilegedId != null && _accessReader.IsAllowed(privilegedId.Value, reader);
    }
}
