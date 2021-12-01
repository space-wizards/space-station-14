using Content.Shared.Hands.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Client.Hands
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHandsComponent))]
    [Friend(typeof(HandsSystem))]
    public class HandsComponent : SharedHandsComponent
    {
        public HandsGui? Gui { get; set; }
    }
}
