using System.Collections.Generic;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Preferences;
using Content.Server.Power.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Content.Server.Storage.Components;
using Content.Server.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using static Content.Shared.Storage.SharedSuitStorageComponent;
using Robust.Shared.Log;

namespace Content.Server.Storage
{
    internal sealed class SuitStorageSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<SuitStorageComponent, InteractHandEvent>(OnSuitStorageInteractHand);
            SubscribeLocalEvent<SuitStorageComponent, InteractUsingEvent>(OnSuitStorageInteractObject);
        }

        public void Update()
        {
            foreach (var (suitStorage, power) in EntityManager.EntityQuery<SuitStorageComponent, ApcPowerReceiverComponent>(true))
            {
                if (suitStorage.UiKnownPowerState != power.Powered)
                {
                    // Must be *before* update
                    suitStorage.UiKnownPowerState = power.Powered;
                    UpdateUserInterface(suitStorage);
                }
            }
        }

        public void UpdateUserInterface(SuitStorageComponent comp)
        {
            comp.UserInterface?.SetState(
                new SuitStorageBoundUserInterfaceState(
                    comp.ContainedItemNameLookup(),
                    comp.Open,
                    comp.UiKnownPowerState
                    ));
        }

        private void OnSuitStorageInteractHand(EntityUid uid, SuitStorageComponent component, InteractHandEvent args)
        {
            UpdateUserInterface(component);
            if (!component.Powered || !args.User.TryGetComponent(out ActorComponent? actor))
                return;

            component.UserInterface?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        private void OnSuitStorageInteractObject(EntityUid uid, SuitStorageComponent component, InteractUsingEvent args)
        {
            component.AddToContents(args.Used);
        }
    }
}
