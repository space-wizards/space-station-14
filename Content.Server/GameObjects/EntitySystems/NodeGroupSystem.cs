using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    public class NodeGroupSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly INodeGroupManager _groupManager;
#pragma warning restore 649

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _groupManager.Update(frameTime);
        }
    }
}
