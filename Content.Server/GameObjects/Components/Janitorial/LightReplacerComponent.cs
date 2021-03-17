#nullable enable
using Content.Server.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Janitorial
{
    /// <summary>
    ///     Device that allows user to quikly change bulbs in <see cref="PoweredLightComponent"/>
    /// </summary>
    [RegisterComponent]
    public class LightReplacerComponent : Component, IAfterInteract
    {
        public override string Name => "LightReplacer";

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            // standard interaction checks
            if (!ActionBlockerSystem.CanUse(eventArgs.User)) return false;
            if (!eventArgs.CanReach) return false;

            // check if it's a powered light
            if (eventArgs.Target == null || !eventArgs.Target.TryGetComponent(out PoweredLightComponent? light)) return false;

            // check if light bulb doesn't need to be replaced
            if (light.LightBulb != null && light.LightBulb.State == LightBulbState.Normal) return false;


            var bulb = Owner.EntityManager.SpawnEntity("LightTube", Owner.Transform.Coordinates);
            return light.ReplacBulb(bulb);
        }
    }
}
