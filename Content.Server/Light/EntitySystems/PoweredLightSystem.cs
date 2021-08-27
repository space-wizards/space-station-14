using Content.Server.Light.Components;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;

namespace Content.Server.Light.Systems
{
    /// <summary>
    ///     System for the PoweredLightComponent. Currently bare-bones, to handle events from the DamageableSystem
    /// </summary>
    public class PoweredLightSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<PoweredLightComponent, DamageChangedEvent>(MaybeDestroyBulb);
        }

        /// <summary>
        /// Destroy the light bulb if the light has any damage.
        /// </summary>
        public static void MaybeDestroyBulb(EntityUid _, PoweredLightComponent component, DamageChangedEvent args)
        {
            if (args.DamageIncreased)
            {
                // Eventually, this logic should all be done by this (or some other) system.
                component.TryDestroyBulb();
            }
        }
    }
}
