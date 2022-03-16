using Content.Server.UserInterface;
using Robust.Server.GameObjects;
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
    }
}
