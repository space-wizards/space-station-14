using Content.Server.GameObjects.Components.Singularity;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Projectiles
{
    [RegisterComponent]
    public class EmitterBoltComponent : Component, ICollideBehavior
    {
        public override string Name => "EmitterBoltComponent";

        void ICollideBehavior.CollideWith(IEntity entity)
        {
            if(entity.TryGetComponent<ContainmentFieldGeneratorComponent>(out var gen))
            {
                gen.Power += 1;
            }
        }
    }
}
