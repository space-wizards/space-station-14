using System.Linq;
using Content.Server.Popups;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using static Content.Shared.Access.Components.AccessOverriderComponent;

namespace Content.Server.Access.Systems;

[UsedImplicitly]
public sealed class AccessOverriderSystem : SharedAccessOverriderSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccessOverriderComponent, WriteToTargetAccessReaderIdMessage>(OnWriteToTargetAccessReaderIdMessage);
        SubscribeLocalEvent<AccessOverriderComponent, ComponentStartup>(UpdateUserInterface);
        SubscribeLocalEvent<AccessOverriderComponent, EntInsertedIntoContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<AccessOverriderComponent, EntRemovedFromContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<AccessOverriderComponent, AfterInteractEvent>(AfterInteractOn);
        SubscribeLocalEvent<AccessOverriderComponent, AccessOverriderDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<AccessOverriderComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<AccessOverriderComponent, BoundUIClosedEvent>(OnClose);
    }

    private void AfterInteractOn(EntityUid uid, AccessOverriderComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || !TryComp(args.Target, out AccessReaderComponent? accessReader))
            return;

        if (!_interactionSystem.InRangeUnobstructed(args.User, (EntityUid) args.Target))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.DoAfter, new AccessOverriderDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnDoAfter(EntityUid uid, AccessOverriderComponent component, AccessOverriderDoAfterEvent args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target != null)
        {
            component.TargetAccessReaderId = args.Args.Target.Value;
            _userInterface.TryOpen(uid, AccessOverriderUiKey.Key, actor.PlayerSession);
            UpdateUserInterface(uid, component, args);
        }

        args.Handled = true;
    }

    private void OnClose(EntityUid uid, AccessOverriderComponent component, BoundUIClosedEvent args)
    {
        if (args.UiKey.Equals(AccessOverriderUiKey.Key))
        {
            component.TargetAccessReaderId = new();
        }
    }

    private void OnWriteToTargetAccessReaderIdMessage(EntityUid uid, AccessOverriderComponent component, WriteToTargetAccessReaderIdMessage args)
    {
        if (args.Session.AttachedEntity is not { Valid: true } player)
            return;

        TryWriteToTargetAccessReaderId(uid, args.AccessList, player, component);

        UpdateUserInterface(uid, component, args);
    }

    private void UpdateUserInterface(EntityUid uid, AccessOverriderComponent component, EntityEventArgs args)
    {
        if (!component.Initialized)
            return;

        var privilegedIdName = string.Empty;
        var targetLabel = Loc.GetString("access-overrider-window-no-target");
        var targetLabelColor = Color.Red;

        string[]? possibleAccess = null;
        string[]? currentAccess = null;
        string[]? missingAccess = null;

        if (component.TargetAccessReaderId is { Valid: true } accessReader)
        {
            targetLabel = Loc.GetString("access-overrider-window-target-label") + " " + EntityManager.GetComponent<MetaDataComponent>(component.TargetAccessReaderId).EntityName;
            targetLabelColor = Color.White;

            List<HashSet<string>> currentAccessHashsets = EntityManager.GetComponent<AccessReaderComponent>(accessReader).AccessLists;
            currentAccess = ConvertAccessHashSetsToList(currentAccessHashsets)?.ToArray();
        }

        if (component.PrivilegedIdSlot.Item is { Valid: true } idCard)
        {
            privilegedIdName = EntityManager.GetComponent<MetaDataComponent>(idCard).EntityName;

            if (component.TargetAccessReaderId is { Valid: true })
            {
                possibleAccess = _accessReader.FindAccessTags(idCard).ToArray();
            }

            if (currentAccess != null && possibleAccess != null)
            {
                missingAccess = currentAccess.Except(possibleAccess).ToArray();
            }
        }

        AccessOverriderBoundUserInterfaceState newState;

        newState = new AccessOverriderBoundUserInterfaceState(
            component.PrivilegedIdSlot.HasItem,
            PrivilegedIdIsAuthorized(uid, component),
            currentAccess,
            possibleAccess,
            missingAccess,
            privilegedIdName,
            targetLabel,
            targetLabelColor);

        _userInterface.TrySetUiState(uid, AccessOverriderUiKey.Key, newState);
    }

    private List<string> ConvertAccessHashSetsToList(List<HashSet<string>> accessHashsets)
    {
        List<string> accessList = new List<string>();

        if (accessHashsets != null && accessHashsets.Any())
        {
            foreach (HashSet<string> hashSet in accessHashsets)
            {
                foreach (string hash in hashSet.ToArray())
                {
                    accessList.Add(hash);
                }
            }
        }

        return accessList;
    }

    private List<HashSet<string>> ConvertAccessListToHashSet(List<string> accessList)
    {
        List<HashSet<string>> accessHashsets = new List<HashSet<string>>();

        if (accessList != null && accessList.Any())
        {
            foreach (string access in accessList)
            {
                accessHashsets.Add(new HashSet<string>() { access });
            }
        }

        return accessHashsets;
    }

    /// <summary>
    /// Called whenever an access button is pressed, adding or removing that access requirement from the target access reader.
    /// </summary>
    private void TryWriteToTargetAccessReaderId(EntityUid uid,
        List<string> newAccessList,
        EntityUid player,
        AccessOverriderComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.TargetAccessReaderId is not { Valid: true })
            return;

        if (!PrivilegedIdIsAuthorized(uid, component))
            return;

        if (!_interactionSystem.InRangeUnobstructed(uid, component.TargetAccessReaderId))
        {
            _popupSystem.PopupEntity(Loc.GetString("access-overrider-out-of-range"), player, player);

            return;
        }

        if (newAccessList.Count > 0 && !newAccessList.TrueForAll(x => component.AccessLevels.Contains(x)))
        {
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to write unknown access tag.");
            return;
        }

        TryComp(component.TargetAccessReaderId, out AccessReaderComponent? accessReader);

        if (accessReader == null)
            return;

        var oldTags = ConvertAccessHashSetsToList(accessReader.AccessLists);
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

        if (!oldTags.ToHashSet().IsSubsetOf(privilegedPerms))
        {
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to modify permissions when they do not have sufficient access!");
            _popupSystem.PopupEntity(Loc.GetString("access-overrider-cannot-modify-access"), player, player);
            _audioSystem.PlayPvs(component.DenialSound, uid);

            return;
        }

        var addedTags = newAccessList.Except(oldTags).Select(tag => "+" + tag).ToList();
        var removedTags = oldTags.Except(newAccessList).Select(tag => "-" + tag).ToList();

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(player):player} has modified {ToPrettyString(component.TargetAccessReaderId):entity} with the following allowed access level holders: [{string.Join(", ", addedTags.Union(removedTags))}] [{string.Join(", ", newAccessList)}]");

        accessReader.AccessLists = ConvertAccessListToHashSet(newAccessList);
        Dirty(accessReader);
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
        return privilegedId != null && _accessReader.IsAllowed(privilegedId.Value, uid, reader);
    }
}
