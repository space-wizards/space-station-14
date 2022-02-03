using Content.Server.UserInterface;
using Content.Shared.HealthScanner;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Medical.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHealthScannerComponent))]
    public class HealthScannerComponent : SharedHealthScannerComponent
    {
        [ViewVariables]
        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(HealthScannerUiKey.Key);
    }
}
