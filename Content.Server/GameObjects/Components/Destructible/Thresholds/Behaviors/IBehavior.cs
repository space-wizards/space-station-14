using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behaviors
{
    public interface IBehavior : IExposeData
    {
        void Execute(IEntity owner, DestructibleSystem system);
    }
}
