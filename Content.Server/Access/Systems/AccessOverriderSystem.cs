using System.Linq;
using Content.Server.Popups;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
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
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!; // Starlight-edit

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccessOverriderComponent, ComponentStartup>(UpdateUserInterface);
        SubscribeLocalEvent<AccessOverriderComponent, EntInsertedIntoContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<AccessOverriderComponent, EntRemovedFromContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<AccessOverriderComponent, AfterInteractEvent>(AfterInteractOn);
        SubscribeLocalEvent<AccessOverriderComponent, AccessOverriderDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<AccessOverriderComponent, AccessGroupSelectedMessage>(OnAccessGroupSelected); // Starlight-edit

        Subs.BuiEvents<AccessOverriderComponent>(AccessOverriderUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(UpdateUserInterface);
            subs.Event<BoundUIClosedEvent>(OnClose);
            subs.Event<WriteToTargetAccessReaderIdMessage>(OnWriteToTargetAccessReaderIdMessage);
        });
    }

    private void AfterInteractOn(EntityUid uid, AccessOverriderComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || !TryComp(args.Target, out AccessReaderComponent? accessReader))
            return;

        if (!_interactionSystem.InRangeUnobstructed(args.User, (EntityUid) args.Target))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.DoAfter, new AccessOverriderDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnDoAfter(EntityUid uid, AccessOverriderComponent component, AccessOverriderDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target != null)
        {
            component.TargetAccessReaderId = args.Args.Target.Value;
            _userInterface.OpenUi(uid, AccessOverriderUiKey.Key, args.User);
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
        if (args.Actor is not { Valid: true } player)
            return;

        TryWriteToTargetAccessReaderId(uid, args.AccessList, player, component);

        UpdateUserInterface(uid, component, args);
    }

    // Starlight-edit: Start
    private void OnAccessGroupSelected(EntityUid uid, AccessOverriderComponent component, AccessGroupSelectedMessage args)
    {
        component.CurrentAccessGroup = args.SelectedGroup;
        UpdateUserInterface(uid, component, args);
    }
    // Starlight-edit: End

    private void UpdateUserInterface(EntityUid uid, AccessOverriderComponent component, EntityEventArgs args)
    {
        if (!component.Initialized)
            return;

        var privilegedIdName = string.Empty;
        var targetLabel = Loc.GetString("access-overrider-window-no-target");
        var targetLabelColor = Color.Red;

        // Starlight. ProtoId<AccessLevelPrototype>[]? possibleAccess = null;
        ProtoId<AccessLevelPrototype>[]? currentAccess = null;
        ProtoId<AccessLevelPrototype>[]? missingAccess = null;

        if (component.TargetAccessReaderId is { Valid: true } accessReader)
        {
            targetLabel = Loc.GetString("access-overrider-window-target-label") + " " + Comp<MetaDataComponent>(component.TargetAccessReaderId).EntityName;
            targetLabelColor = Color.White;

            if (!_accessReader.GetMainAccessReader(accessReader, out var accessReaderEnt))
                return;

            var currentAccessHashsets = accessReaderEnt.Value.Comp.AccessLists;
            currentAccess = ConvertAccessHashSetsToList(currentAccessHashsets).ToArray();
        }
        ProtoId<AccessLevelPrototype>[]? possibleAccess = Array.Empty<ProtoId<AccessLevelPrototype>>(); // Starlight

        if (component.PrivilegedIdSlot.Item is { Valid: true } idCard)
        {
            privilegedIdName = Comp<MetaDataComponent>(idCard).EntityName;
            // Starlight-edit: Start
            var privTags = _accessReader.FindAccessTags(idCard).ToArray();
            possibleAccess = privTags;

            if (component.AccessGroups != null && component.AccessGroups.Count > 0)
            {
                if (component.CurrentAccessGroup == null)
                {
                    var bestGroup = component.AccessGroups
                        .Select(g => new 
                        { 
                            Group = g, 
                            MatchCount = _prototypeManager.TryIndex(g, out AccessGroupPrototype? gp) 
                                ? gp.Tags.Count(tag => privTags.Contains(tag)) 
                                : 0,
                            TotalCount = _prototypeManager.TryIndex(g, out AccessGroupPrototype? gp2)
                                ? gp2.Tags.Count(tag => 
                                    _prototypeManager.TryIndex<AccessLevelPrototype>(tag, out var accessProto) && 
                                    accessProto.CanAddToIdCard)
                                : 0
                        })
                        .Where(x => x.MatchCount > 0 && x.MatchCount >= Math.Max(1, Math.Min(3, x.TotalCount / 2)))
                        .OrderByDescending(x => x.MatchCount)
                        .FirstOrDefault()?.Group;

                    component.CurrentAccessGroup = bestGroup ?? component.AccessGroups.First();
                }
            }
            
            if (currentAccess != null)
                missingAccess = currentAccess.Except(privTags).ToArray();
        }
        else
        {
            privilegedIdName = string.Empty;
            missingAccess = currentAccess; 
        }

        var groupsArray = component.AccessGroups?.ToArray();
        var newState = new AccessOverriderBoundUserInterfaceState(
            component.PrivilegedIdSlot.HasItem,
            PrivilegedIdIsAuthorized(uid, component),
            currentAccess,
            possibleAccess, // Starlight
            missingAccess,
            privilegedIdName,
            targetLabel,
            targetLabelColor,
            groupsArray,
            component.CurrentAccessGroup); // Starlight

        _userInterface.SetUiState(uid, AccessOverriderUiKey.Key, newState);
    }

    private List<ProtoId<AccessLevelPrototype>> ConvertAccessHashSetsToList(List<HashSet<ProtoId<AccessLevelPrototype>>> accessHashsets)
    {
        var accessList = new List<ProtoId<AccessLevelPrototype>>();

        if (accessHashsets.Count <= 0)
            return accessList;

        foreach (var hashSet in accessHashsets)
        {
            accessList.AddRange(hashSet);
        }

        return accessList;
    }

    /// <summary>
    /// Called whenever an access button is pressed, adding or removing that access requirement from the target access reader.
    /// </summary>
    private void TryWriteToTargetAccessReaderId(EntityUid uid,
        List<ProtoId<AccessLevelPrototype>> newAccessList,
        EntityUid player,
        AccessOverriderComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.TargetAccessReaderId is not { Valid: true })
            return;

        if (!PrivilegedIdIsAuthorized(uid, component))
            return;

        if (!_interactionSystem.InRangeUnobstructed(player, component.TargetAccessReaderId))
        {
            _popupSystem.PopupEntity(Loc.GetString("access-overrider-out-of-range"), player, player);

            return;
        }

        // Starlight-edit: Start
        if (component.AccessGroups != null && component.AccessGroups.Count > 0)
        {
            // flatten all group tags
            var allGroupTags = new HashSet<ProtoId<AccessLevelPrototype>>();
            foreach (var g in component.AccessGroups)
            {
                if (!_prototypeManager.TryIndex(g, out AccessGroupPrototype? gp))
                    continue;
                allGroupTags.UnionWith(gp.Tags);
            }

            if (newAccessList.Count > 0 && !newAccessList.TrueForAll(x => allGroupTags.Contains(x)))
            {
                _sawmill.Warning($"User {ToPrettyString(uid)} tried to write unknown access tag.");
                return;
            }
        }
        else
        {
            if (newAccessList.Count > 0 && !newAccessList.TrueForAll(x => component.AccessLevels.Contains(x)))
        // Starlight-edit: End
            {
                _sawmill.Warning($"User {ToPrettyString(uid)} tried to write unknown access tag.");
                return;
            }
        }

        if (!_accessReader.GetMainAccessReader(component.TargetAccessReaderId, out var accessReaderEnt))
            return;

        var oldTags = ConvertAccessHashSetsToList(accessReaderEnt.Value.Comp.AccessLists);
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

        _adminLogger.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(player):player} has modified {ToPrettyString(accessReaderEnt.Value):entity} with the following allowed access level holders: [{string.Join(", ", addedTags.Union(removedTags))}] [{string.Join(", ", newAccessList)}]");

        _accessReader.SetAccesses(accessReaderEnt.Value, newAccessList);

        var ev = new OnAccessOverriderAccessUpdatedEvent(player);
        RaiseLocalEvent(component.TargetAccessReaderId, ref ev);
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

        if (!_accessReader.GetMainAccessReader(uid, out var accessReader)) // Starlight edit
            return true;

        var privilegedId = component.PrivilegedIdSlot.Item;
        return privilegedId != null && _accessReader.IsAllowed(privilegedId.Value, uid, accessReader.Value.Comp); // Starlight edit
    }
}
