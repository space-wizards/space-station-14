using Content.Shared.Interaction;
using Content.Server.Power.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Content.Server.Storage.Components;
using static Content.Shared.Storage.SharedSuitStorageComponent;

namespace Content.Server.Storage
{
    internal sealed class SuitStorageSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<SuitStorageComponent, InteractHandEvent>(OnSuitStorageInteractHand);
            SubscribeLocalEvent<SuitStorageComponent, InteractUsingEvent>(OnSuitStorageInteractObject);
            SubscribeLocalEvent<SuitStorageComponent, PowerChangedEvent>(OnPowerChanged);
        }

        public void UpdateUserInterface(SuitStorageComponent comp)
        {
            foreach (var suitStorage in EntityManager.EntityQuery<SuitStorageComponent>(true))
            {
                bool powered = suitStorage.Powered;
                if(suitStorage.UiKnownPowerState != powered){
                    suitStorage.UiKnownPowerState = powered;
                    UpdateUserInterface(suitStorage);
                }
            }

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
            if (!args.User.TryGetComponent(out ActorComponent? actor))
                return;

            component.UserInterface?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        private void OnSuitStorageInteractObject(EntityUid uid, SuitStorageComponent component, InteractUsingEvent args)
        {
            component.AddToContents(args.Used, args.User);
        }

        private void OnPowerChanged(EntityUid uid, SuitStorageComponent component, PowerChangedEvent args){
            UpdateUserInterface(component);
        }
    }
}
