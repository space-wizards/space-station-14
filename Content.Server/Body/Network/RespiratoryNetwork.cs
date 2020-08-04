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
            Owner.EnsureComponent<LungsComponent>();
        }

        public override void OnRemove()
        {
            if (Owner.HasComponent<LungsComponent>())
            {
                Owner.RemoveComponent<LungsComponent>();
            }
        }

        public override void Update(float frameTime)
        {
            Owner.GetComponent<LungsComponent>().Update(frameTime);
        }
    }
}
