#nullable enable
using Content.Server.Power.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Power.EntitySystems
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
