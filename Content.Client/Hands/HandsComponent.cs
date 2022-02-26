using Content.Shared.Hands.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Hands
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHandsComponent))]
    [Friend(typeof(HandsSystem))]
    public sealed class HandsComponent : SharedHandsComponent
    {
        public HandsGui? Gui { get; set; }
    }
}
