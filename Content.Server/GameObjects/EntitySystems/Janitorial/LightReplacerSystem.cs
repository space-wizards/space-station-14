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

            SubscribeLocalEvent<LightReplacerComponent, InteractUsingEvent>(HandleInteract);
            SubscribeLocalEvent<LightReplacerComponent, AfterInteractEvent>(HandleAfterInteract);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<LightReplacerComponent, InteractUsingEvent>(HandleInteract);
            UnsubscribeLocalEvent<LightReplacerComponent, AfterInteractEvent>(HandleAfterInteract);
        }

        private void HandleAfterInteract(EntityUid uid, LightReplacerComponent component, AfterInteractEvent eventArgs)
        {
            // standard interaction checks
            if (!ActionBlockerSystem.CanUse(eventArgs.User)) return;
            if (!eventArgs.CanReach) return;

            // behaviour will depends on target type
            if (eventArgs.Target != null)
            {
                // replace broken light in fixture?
                if (eventArgs.Target.TryGetComponent(out PoweredLightComponent? fixture))
                    component.TryReplaceBulb(fixture, eventArgs.User);
                // add new bulb to light replacer container?
                else if (eventArgs.Target.TryGetComponent(out LightBulbComponent? bulb))
                    component.TryInsertBulb(bulb, eventArgs.User, true);
            }
        }

        private void HandleInteract(EntityUid uid, LightReplacerComponent component, InteractUsingEvent eventArgs)
        {
            // standard interaction checks
            if (!ActionBlockerSystem.CanInteract(eventArgs.User)) return;

            if (eventArgs.Used != null)
            {
                // want to insert a new light bulb?
                if (eventArgs.Used.TryGetComponent(out LightBulbComponent? bulb))
                    component.TryInsertBulb(bulb, eventArgs.User, true);
                // add bulbs from storage?
                else if (eventArgs.Used.TryGetComponent(out ServerStorageComponent? storage))
                    component.TryInsertBulb(storage, eventArgs.User);
            }
        }
    }
}
