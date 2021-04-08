#nullable enable
using System;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Janitorial;
using Content.Server.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.Janitorial
{
    [UsedImplicitly]
    public class LightReplacerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LightReplacerComponent, InteractUsingMessage>(HandleInteract);
            SubscribeLocalEvent<LightReplacerComponent, AfterInteractMessage>(HandleAfterInteract);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<LightReplacerComponent, InteractUsingMessage>(HandleInteract);
            UnsubscribeLocalEvent<LightReplacerComponent, AfterInteractMessage>(HandleAfterInteract);
        }

        private void HandleAfterInteract(EntityUid uid, LightReplacerComponent component, AfterInteractMessage eventArgs)
        {
            // standard interaction checks
            if (!ActionBlockerSystem.CanUse(eventArgs.User)) return;
            if (!eventArgs.CanReach) return;

            // behaviour will depends on target type
            if (eventArgs.Attacked != null)
            {
                // replace broken light in fixture?
                if (eventArgs.Attacked.TryGetComponent(out PoweredLightComponent? fixture))
                    component.TryReplaceBulb(fixture, eventArgs.User);
                // add new bulb to light replacer container?
                else if (eventArgs.Attacked.TryGetComponent(out LightBulbComponent? bulb))
                    component.TryInsertBulb(bulb, eventArgs.User, true);
            }
        }

        private void HandleInteract(EntityUid uid, LightReplacerComponent component, InteractUsingMessage eventArgs)
        {
            // standard interaction checks
            if (!ActionBlockerSystem.CanInteract(eventArgs.User)) return;

            if (eventArgs.ItemInHand != null)
            {
                // want to insert a new light bulb?
                if (eventArgs.ItemInHand.TryGetComponent(out LightBulbComponent? bulb))
                    component.TryInsertBulb(bulb, eventArgs.User, true);
                // add bulbs from storage?
                else if (eventArgs.ItemInHand.TryGetComponent(out ServerStorageComponent? storage))
                    component.TryInsertBulb(storage, eventArgs.User);
            }
        }
    }
}
