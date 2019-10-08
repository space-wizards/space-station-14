using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public class ServerStatusEffectsComponent : SharedStatusEffectsComponent
    {
        // A dummy component just so other components are able to send messages to the ClientOverlayEffects
    }
}
