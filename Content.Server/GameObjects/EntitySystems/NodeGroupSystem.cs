using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
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
