using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behavior
{
    public interface IThresholdBehavior : IExposeData
    {
        void Trigger(IEntity owner, DestructibleSystem system);
    }
}
