using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    public class PowerNetSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IPowerNetManager _powerNetManager;
#pragma warning restore 649

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _powerNetManager.Update(frameTime);
        }
    }
}
