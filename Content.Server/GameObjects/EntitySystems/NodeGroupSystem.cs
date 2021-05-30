using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class NodeGroupSystem : EntitySystem
    {
        [Dependency] private readonly INodeGroupManager _groupManager = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _groupManager.Update(frameTime);
        }
    }
}
