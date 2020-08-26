using Content.Server.GameObjects.Components.Body.Digestive;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.Network
{
    /// <summary>
    ///     Represents the system that processes food, liquids, and the reagents inside them.
    /// </summary>
    [UsedImplicitly]
    public class DigestiveNetwork : BodyNetwork
    {
        public override string Name => "Digestive";

        protected override void OnAdd()
        {
            Owner.EnsureComponent<StomachComponent>();
        }

        public override void OnRemove()
        {
            if (Owner.HasComponent<StomachComponent>())
            {
                Owner.RemoveComponent<StomachComponent>();
            }
        }
    }
}
