using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Access.Systems
{
    public abstract partial class SharedAccessOverriderSystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly ILogManager _log = default!;
        [Dependency] protected readonly IGameTiming Timing = default!;
        [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;
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

            SubscribeLocalEvent<AccessOverriderComponent, EntInsertedIntoContainerMessage>(UpdateUserInterface);
            SubscribeLocalEvent<AccessOverriderComponent, EntRemovedFromContainerMessage>(UpdateUserInterface);

            SubscribeLocalEvent<AccessOverriderComponent, AfterInteractEvent>(AfterInteractOn);
            SubscribeLocalEvent<AccessOverriderComponent, AccessOverriderDoAfterEvent>(OnDoAfter);

            SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);

            Subs.BuiEvents<AccessOverriderComponent>(AccessOverriderComponent.AccessOverriderUiKey.Key, subs =>
            {
                // It kinda drives me nuts that I need to subscribe to the BUI
                // open but if you don't you get so many prediction issues due
                // to ordering of BUI opens versus updates and I lost so many
                // hours of my life trying to fix those rather than just
                // subscribing.
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

        private void UpdateUserInterface<T>(Entity<AccessOverriderComponent> ent, ref T args)
        {
            DirtyUI(ent);
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

            args.Handled = true;

            if (args.Args.Target == null)
                return;

            component.TargetAccessReaderId = args.Target;
            Dirty(uid, component);
            UI.OpenUi(uid, AccessOverriderComponent.AccessOverriderUiKey.Key, args.User);
        }

        private void OnClose(EntityUid uid, AccessOverriderComponent component, BoundUIClosedEvent args)
        {
            if (!Timing.IsFirstTimePredicted || !args.UiKey.Equals(AccessOverriderComponent.AccessOverriderUiKey.Key))
                return;

            component.TargetAccessReaderId = null;
            Dirty(uid, component);
        }

        private void OnWriteToTargetAccessReaderIdMessage(EntityUid uid, AccessOverriderComponent component, AccessOverriderComponent.WriteToTargetAccessReaderIdMessage args)
        {
            if (args.Actor is not { Valid: true } player)
                return;

            TryWriteToTargetAccessReaderId(uid, args.AccessList, player, component);

            DirtyUI(uid);
        }

        /// <summary>
        /// Called whenever an access button is pressed, adding or removing that access requirement from the target access reader.
        /// </summary>
        private void TryWriteToTargetAccessReaderId(EntityUid uid,
            List<ProtoId<AccessLevelPrototype>> newAccessList,
            EntityUid player,
            AccessOverriderComponent? component = null)
        {
            if (!Resolve(uid, ref component) || component.TargetAccessReaderId is not { } readerId)
                return;

            if (!PrivilegedIdIsAuthorized(uid, component))
                return;

            if (!_interactionSystem.InRangeUnobstructed(player, readerId))
            {
                _popupSystem.PopupClient(Loc.GetString("access-overrider-out-of-range"), player, player);

                return;
            }

            if (newAccessList.Count > 0 && !newAccessList.TrueForAll(x => component.AccessLevels.Contains(x)))
            {
                _sawmill.Warning($"User {ToPrettyString(player)} tried to write unknown access tag.");
                return;
            }

            if (!_accessReader.GetMainAccessReader(readerId, out var accessReaderEnt))
                return;

            var oldTags = accessReaderEnt.Value.Comp.AccessLists.SelectMany(x => x).ToList();
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
            RaiseLocalEvent(readerId, ref ev);
        }

        /// <summary>
        /// Returns true if there is an ID in <see cref="AccessOverriderComponent.PrivilegedIdSlot"/> and said ID satisfies the requirements of <see cref="AccessReaderComponent"/>.
        /// </summary>
        /// <remarks>
        /// Other code relies on the fact this returns false if privileged Id is null. Don't break that invariant.
        /// </remarks>
        public bool PrivilegedIdIsAuthorized(EntityUid uid, AccessOverriderComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return true;

            if (_accessReader.GetMainAccessReader(uid, out var accessReader))
                return true;

            var privilegedId = component.PrivilegedIdSlot.Item;
            return privilegedId != null && _accessReader.IsAllowed(privilegedId.Value, uid, accessReader);
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

        protected virtual void DirtyUI(EntityUid uid) { }

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
