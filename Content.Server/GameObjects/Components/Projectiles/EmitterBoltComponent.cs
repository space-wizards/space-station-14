using Content.Server.GameObjects.Components.Singularity;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Projectiles
{
    [RegisterComponent]
    public class EmitterBoltComponent : Component
    {
        public override string Name => "EmitterBoltComponent";
    }
}
