using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Destructible.Threshold.Behavior
{
    public interface IThresholdBehavior
    {
        void Trigger(IEntity owner, DestructibleSystem system);
    }
}