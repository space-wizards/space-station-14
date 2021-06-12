#nullable enable
using Content.Server.APC.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.APC
{
    [UsedImplicitly]
    internal sealed class PowerApcSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var apc in ComponentManager.EntityQuery<ApcComponent>(false))
            {
                apc.Update();
            }
        }
    }
}
