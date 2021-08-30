using Content.Server.Light.Components;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Content.Server.MachineLinking.Events;

namespace Content.Server.Light.EntitySystems
{
    /// <summary>
    ///     System for the PoweredLightComponent. Currently bare-bones, to handle events from the DamageableSystem
    /// </summary>
    public class PoweredLightSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PoweredLightComponent, SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<PoweredLightComponent, DamageChangedEvent>(HandleLightDamaged);
        }

        /// <summary>
        ///     Destroy the light bulb if the light took any damage.
        /// </summary>
        public static void HandleLightDamaged(EntityUid uid, PoweredLightComponent component, DamageChangedEvent args)
        {
            // Was it being repaired, or did it take damage?
            if (args.DamageIncreased)
            {
                // Eventually, this logic should all be done by this (or some other) system, not a component.
                component.TryDestroyBulb();
            }
        }

        private void OnSignalReceived(EntityUid uid, PoweredLightComponent component, SignalReceivedEvent args)
        {
            switch (args.Port)
            {
                case "toggle":
                    component.ToggleLight();
                    break;
            }
        }
    }
}
