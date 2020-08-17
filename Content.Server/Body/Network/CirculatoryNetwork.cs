using Content.Server.GameObjects.Components.Body.Circulatory;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.Network
{
    [UsedImplicitly]
    public class CirculatoryNetwork : BodyNetwork
    {
        public override string Name => "Circulatory";

        protected override void OnAdd()
        {
            Owner.EnsureComponent<BloodstreamComponent>();
        }

        public override void OnRemove()
        {
            if (Owner.HasComponent<BloodstreamComponent>())
            {
                Owner.RemoveComponent<BloodstreamComponent>();
            }
        }
    }
}
