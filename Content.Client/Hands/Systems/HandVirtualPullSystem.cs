using Content.Client.Items;
using Content.Shared.Hands.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Client.Hands
{
    [UsedImplicitly]
    public sealed class HandVirtualPullSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            Subs.ItemStatus<HandVirtualPullComponent>(_ => new HandVirtualPullItemStatus());
        }
    }
}
