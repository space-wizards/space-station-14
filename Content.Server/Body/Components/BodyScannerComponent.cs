using Content.Shared.Body.Components;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyScannerComponent))]
    public sealed class BodyScannerComponent : SharedBodyScannerComponent
    {
    }
}
