using Content.Shared.Movement;
using Robust.Shared.GameObjects;
using Content.Server.Storage.Components;
using Content.Server.DoAfter;
using Content.Shared.Popups;
using Robust.Shared.Localization;

namespace Content.Server.Resist
{
    public class ResistLockerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ResistLockerComponent, RelayMovementEntityEvent>(OnRelayMovement);
        }


        private void OnRelayMovement(EntityUid uid, ResistLockerComponent component, RelayMovementEntityEvent args)
        {
            TryComp(uid, out EntityStorageComponent? storageComponent);
            if (storageComponent == null)
                return;

            if (!component.IsResisting)
            {
                //Check IfWeldedShut or if Lock exists and is locked
                if (TryComp<LockComponent?>(uid, out var lockComponent) && lockComponent.Locked || storageComponent.IsWeldedShut)
                {
                    AttemptResist(args.Entity, uid, storageComponent, component);
                } 
            }

        }
        private async void AttemptResist(EntityUid user, EntityUid target, EntityStorageComponent? storageComponent, ResistLockerComponent? resistLockerComponent)
        {
            if (!Resolve(target, ref storageComponent, ref resistLockerComponent))
                return;

            var doAfterEventArgs = new DoAfterEventArgs(user, resistLockerComponent.ResistTime, default, target)
            {
                BreakOnTargetMove = false,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = false //No hands 'cause we be kickin'
            };

            resistLockerComponent.IsResisting = true;
            user.PopupMessage(Loc.GetString("resist-locker-component-start-resisting"));

            var result = await EntitySystem.Get<DoAfterSystem>().WaitDoAfter(doAfterEventArgs);

            resistLockerComponent.IsResisting = false;

            if(result != DoAfterStatus.Cancelled)
            {
                if (storageComponent.IsWeldedShut)
                {
                    storageComponent.IsWeldedShut = false;
                }

                //Handle Locks as well
                if (TryComp<LockComponent>(target, out var lockComponent))
                {
                    lockComponent.Locked = false;
                }

                storageComponent.TryOpenStorage(user);
                return;
            }
            user.PopupMessage(Loc.GetString("resist-locker-component-resist-interrupted"));
        }

    }
}
