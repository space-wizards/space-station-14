using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public class DisposalTransitComponent : DisposalTubeComponent
    {
        public override string Name => "DisposalTransit";
    }
}
