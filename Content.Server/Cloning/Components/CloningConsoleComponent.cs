using Content.Server.UserInterface;
using Robust.Server.GameObjects;
using Content.Server.Medical.Components;
using Content.Server.Cloning.Components;
using Content.Shared.Cloning.CloningConsole;

namespace Content.Server.Cloning.CloningConsole
{
    [RegisterComponent]
    public sealed class CloningConsoleComponent : Component
    {
        [ViewVariables]
        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(CloningConsoleUiKey.Key);
        [ViewVariables]
        public EntityUid? GeneticScanner = null;
        [ViewVariables]
        public EntityUid? CloningPod = null;
        [ViewVariables]
        public List<String> CloningHistory = new List<string>();
    }
}
