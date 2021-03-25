using Content.Client.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems;

namespace Content.Client.GameObjects.EntitySystems
{
    internal sealed class HandsSystem : SharedHandsSystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var hands in ComponentManager.EntityQuery<HandsComponent>(false))
            {
                hands.RefreshHands(); //temp hack to fix updating container when containers change
            }
        }
    }
}
