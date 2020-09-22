using Content.Server.GameObjects.Components.Body.Respiratory;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.Network
{
    [UsedImplicitly]
    public class RespiratoryNetwork : BodyNetwork
    {
        public override string Name => "Respiratory";

        protected override void OnAdd()
        {
            Owner.EnsureComponent<LungComponent>();
        }

        public override void OnRemove()
        {
            if (Owner.HasComponent<LungComponent>())
            {
                Owner.RemoveComponent<LungComponent>();
            }
        }
    }
}
