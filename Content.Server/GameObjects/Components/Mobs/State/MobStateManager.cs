using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.State
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMobStateComponent))]
    [ComponentReference(typeof(IMobStateComponent))]
    public class MobStateComponent : SharedMobStateComponent
    {
    }
}
