using Content.Server.UserInterface;
using Content.Shared.CloningConsole;
using Robust.Server.GameObjects;
using Content.Server.Medical.GeneticScanner;
using Content.Server.Cloning.Components;

namespace Content.Server.Medical.Components
{
    [RegisterComponent]
    public class CloningConsoleComponent : SharedCloningConsoleComponent
    {
        [ViewVariables]
        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(CloningConsoleUiKey.Key);
        public GeneticScannerComponent? GeneticScanner = null;
        public CloningPodComponent? CloningPod = null;
        public List<String> CloningHistory = new List<string>();
    }
}
