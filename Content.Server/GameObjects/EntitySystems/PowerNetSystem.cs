using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class PowerNetSystem : EntitySystem
    {
        [Dependency] private readonly IPowerNetManager _powerNetManager = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _powerNetManager.Update(frameTime);
        }
    }
}
