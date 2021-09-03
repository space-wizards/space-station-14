using Content.Server.Atmos.Components;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Storage.Components;
using Content.Shared.Interaction;
using Content.Shared.PneumaticCannon;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.PneumaticCannon
{
    public class PneumaticCannonSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PneumaticCannonComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<PneumaticCannonComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<PneumaticCannonComponent, InteractHandEvent>(OnInteractHand);
        }

        private void OnComponentInit(EntityUid uid, PneumaticCannonComponent component, ComponentInit args)
        {
            component.GasTankSlot = component.Owner.EnsureContainer<ContainerSlot>($"{component.Name}-gasTank");
        }

        private void OnInteractUsing(EntityUid uid, PneumaticCannonComponent component, InteractUsingEvent args)
        {
            if (args.Used.HasComponent<GasTankComponent>() && component.GasTankSlot.CanInsert(args.Used))
            {
                component.GasTankSlot.Insert(args.Used);
                // popup
                UpdateAppearance(component);
                return;
            }

            if (args.Used.TryGetComponent<ItemComponent>(out var item)
                && component.Owner.TryGetComponent<ServerStorageComponent>(out var storage))
            {
                if (storage.CanInsert(args.Used))
                {
                    storage.Insert(args.Used);
                    // popup
                }
                else
                {
                    // popup
                }
            }
        }

        private void OnInteractHand(EntityUid uid, PneumaticCannonComponent component, InteractHandEvent args)
        {
            TryRemoveGasTank(component, args.User);
        }

        public void TryRemoveGasTank(PneumaticCannonComponent component, IEntity user)
        {
            if (component.GasTankSlot.ContainedEntity == null)
            {
                //popup
                return;
            }

            var ent = component.GasTankSlot.ContainedEntity;
            if (component.GasTankSlot.Remove(ent))
            {
                if (user.TryGetComponent<HandsComponent>(out var hands))
                {
                    hands.TryPickupEntityToActiveHand(ent);
                }

                //popup
                UpdateAppearance(component);
            }
        }

        private void UpdateAppearance(PneumaticCannonComponent component)
        {
            if (component.Owner.TryGetComponent<AppearanceComponent>(out var appearance))
            {
                appearance.SetData(PneumaticCannonVisuals.Tank,
                    component.GasTankSlot.ContainedEntities.Count != 0);
            }
        }
    }
}
