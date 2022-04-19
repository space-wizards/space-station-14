using Content.Server.UserInterface;
using Robust.Server.GameObjects;
using Content.Shared.Cloning.CloningConsole;

namespace Content.Server.Cloning.CloningConsole
{
    [RegisterComponent]
    public sealed class CloningConsoleComponent : Component
    {
        [ViewVariables]
        public EntityUid? GeneticScanner = null;
        [ViewVariables]
        public EntityUid? CloningPod = null;
    }
}
