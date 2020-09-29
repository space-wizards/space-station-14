using Content.Shared.GameObjects.Components.Disposal;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Disposal
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedDisposalUnitComponent))]
    public class DisposalUnitComponent : SharedDisposalUnitComponent
    {
    }
}
