using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Triggers
{
    public interface ITrigger : IExposeData
    {
        bool Reached(IDamageableComponent damageable, DestructibleSystem system);
    }
}
