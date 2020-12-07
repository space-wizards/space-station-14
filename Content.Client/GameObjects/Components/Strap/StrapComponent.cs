#nullable enable
using Content.Shared.GameObjects.Components.Strap;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Strap
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStrapComponent))]
    public class StrapComponent : SharedStrapComponent
    {
    }
}
