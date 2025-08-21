using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Systems
{
    [UsedImplicitly]
    public abstract partial class SharedAccessOverriderSystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly ILogManager _log = default!;
        [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

        public const string Sawmill = "accessoverrider";
        protected ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = _log.GetSawmill(Sawmill);

            SubscribeLocalEvent<AccessOverriderComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<AccessOverriderComponent, ComponentRemove>(OnComponentRemove);

            SubscribeLocalEvent<AccessOverriderComponent, ComponentStartup>(UpdateUserInterface);
            SubscribeLocalEvent<AccessOverriderComponent, EntInsertedIntoContainerMessage>(UpdateUserInterface);
            SubscribeLocalEvent<AccessOverriderComponent, EntRemovedFromContainerMessage>(UpdateUserInterface);
            SubscribeLocalEvent<AccessOverriderComponent, AfterInteractEvent>(AfterInteractOn);
            SubscribeLocalEvent<AccessOverriderComponent, AccessOverriderDoAfterEvent>(OnDoAfter);

            Subs.BuiEvents<AccessOverriderComponent>(AccessOverriderComponent.AccessOverriderUiKey.Key, subs =>
            {
                subs.Event<BoundUIOpenedEvent>(UpdateUserInterface);
                subs.Event<BoundUIClosedEvent>(OnClose);
                subs.Event<AccessOverriderComponent.WriteToTargetAccessReaderIdMessage>(OnWriteToTargetAccessReaderIdMessage);
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
                Dirty(uid, component);
                _userInterface.OpenUi(uid, AccessOverriderComponent.AccessOverriderUiKey.Key, args.User);
                UpdateUserInterface(uid, component, args);
            }

            args.Handled = true;
        }

        private void OnClose(EntityUid uid, AccessOverriderComponent component, BoundUIClosedEvent args)
        {
            if (args.UiKey.Equals(AccessOverriderComponent.AccessOverriderUiKey.Key))
            {
                component.TargetAccessReaderId = new();
            }
        }

        private void OnWriteToTargetAccessReaderIdMessage(EntityUid uid, AccessOverriderComponent component, AccessOverriderComponent.WriteToTargetAccessReaderIdMessage args)
        {
            if (args.Actor is not { Valid: true } player)
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

            ProtoId<AccessLevelPrototype>[]? possibleAccess = null;
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

            if (component.PrivilegedIdSlot.Item is { Valid: true } idCard)
            {
                privilegedIdName = Comp<MetaDataComponent>(idCard).EntityName;

                if (component.TargetAccessReaderId is { Valid: true })
                {
                    possibleAccess = _accessReader.FindAccessTags(idCard).ToArray();
                }

                if (currentAccess != null && possibleAccess != null)
                {
                    missingAccess = currentAccess.Except(possibleAccess).ToArray();
                }
            }

            AccessOverriderComponent.AccessOverriderBoundUserInterfaceState newState;

            newState = new AccessOverriderComponent.AccessOverriderBoundUserInterfaceState(
                component.PrivilegedIdSlot.HasItem,
                PrivilegedIdIsAuthorized(uid, component),
                currentAccess,
                possibleAccess,
                missingAccess,
                privilegedIdName,
                targetLabel,
                targetLabelColor);

            _userInterface.SetUiState(uid, AccessOverriderComponent.AccessOverriderUiKey.Key, newState);
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
                _popupSystem.PopupClient(Loc.GetString("access-overrider-out-of-range"), player, player);

                return;
            }

            if (newAccessList.Count > 0 && !newAccessList.TrueForAll(x => component.AccessLevels.Contains(x)))
            {
                _sawmill.Warning($"User {ToPrettyString(player)} tried to write unknown access tag.");
                return;
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
                _sawmill.Warning($"User {ToPrettyString(player)} tried to modify permissions they could not give/take!");

                return;
            }

            if (!oldTags.ToHashSet().IsSubsetOf(privilegedPerms))
            {
                _sawmill.Warning($"User {ToPrettyString(player)} tried to modify permissions when they do not have sufficient access!");
                _popupSystem.PopupClient(Loc.GetString("access-overrider-cannot-modify-access"), player, player);
                _audioSystem.PlayPredicted(component.DenialSound, uid, player);

                return;
            }

            var addedTags = newAccessList.Except(oldTags).Select(tag => "+" + tag).ToList();
            var removedTags = oldTags.Except(newAccessList).Select(tag => "-" + tag).ToList();

            _adminLogger.Add(LogType.Action,
                LogImpact.High,
                $"{ToPrettyString(player):player} has modified "
                + $"{ToPrettyString(accessReaderEnt.Value):entity} with the following allowed access level holders: "
                + $"[{string.Join(", ", addedTags.Union(removedTags))}] [{string.Join(", ", newAccessList)}]");

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

            if (_accessReader.GetMainAccessReader(uid, out var accessReader))
                return true;

            var privilegedId = component.PrivilegedIdSlot.Item;
            return privilegedId != null && _accessReader.IsAllowed(privilegedId.Value, uid, accessReader);
        }

        [Serializable, NetSerializable]
        public sealed partial class AccessOverriderDoAfterEvent : DoAfterEvent
        {
            public AccessOverriderDoAfterEvent()
            {
            }

            public override DoAfterEvent Clone() => this;
        }
    }
}

[ByRefEvent]
public record struct OnAccessOverriderAccessUpdatedEvent(EntityUid UserUid, bool Handled = false);
