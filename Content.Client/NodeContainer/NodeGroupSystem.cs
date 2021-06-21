using Content.Shared.NodeContainer;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Client.NodeContainer
{
    [UsedImplicitly]
    public sealed class NodeGroupSystem : EntitySystem
    {
        public bool VisEnabled { get; private set; }

        public override void Initialize()
        {
            base.Initialize();


        }

        public void SetVisEnabled(bool enabled)
        {
            VisEnabled = enabled;

            RaiseNetworkEvent(new NodeVis.MsgEnable(enabled));
        }


    }
}
