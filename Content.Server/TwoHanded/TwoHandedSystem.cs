using Content.Server.TwoHanded.Components;
using Content.Server.Hands.Systems;
using Content.Server.Hands.Components;
using Content.Server.Popups;
using Content.Shared.Item;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Player;

namespace Content.Server.TwoHanded;
    internal sealed class TwoHandedSystem : EntitySystem
    {
        [Dependency] private readonly HandVirtualItemSystem _virtualItemSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TwoHandedComponent,GettingPickedUpAttemptEvent>(PickupAttempt);
            SubscribeLocalEvent<TwoHandedComponent,GotEquippedHandEvent>(OnEquipped);
            SubscribeLocalEvent<TwoHandedComponent,GotUnequippedHandEvent>(OnDeEquipped);
            SubscribeLocalEvent<TwoHandedComponent,VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        }

        private void PickupAttempt(EntityUid uid, TwoHandedComponent comp, GettingPickedUpAttemptEvent args)
        {
            if (args.User == null)
                return;
            if (!TryComp<HandsComponent>(args.User, out var hands) || hands is null)
                return;
            foreach (var hand in hands.Hands.Values)
            {
                if (hand.IsEmpty == true)
                    continue;
                args.Cancel();
                _popupSystem.PopupEntity(Loc.GetString("item-to-heavy"), uid, args.User);
            }
        }
        private void OnEquipped(EntityUid uid, TwoHandedComponent comp, GotEquippedHandEvent args)
        {
            if (args.User == null)
                return;
            if (!TryComp<HandsComponent>(args.User, out var hands) || hands is null)
                return;
            _virtualItemSystem.TrySpawnVirtualItemInHand(args.Equipped, args.User);
        }

        private void OnDeEquipped(EntityUid uid, TwoHandedComponent comp, GotUnequippedHandEvent args)
        {
            if (args.User == null)
                return;
            _virtualItemSystem.DeleteInHandsMatching(args.User, args.Unequipped);
        }

        private void OnVirtualItemDeleted(EntityUid uid, TwoHandedComponent comp, VirtualItemDeletedEvent args)
        {
            if (args.User == null)
                return;
            if (!TryComp<HandsComponent>(args.User, out var hands) || hands is null)
                return;
            if (args.BlockingEntity == uid)
            {
                foreach(var hand in hands.Hands)
                {
                    if (hand.Value != hands.ActiveHand!)
                    {
                        _handsSystem.TryDrop(args.User, hand.Value, null, checkActionBlocker: true);
                    }
                }
            }
        }
    }
