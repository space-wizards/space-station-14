using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Nuke
{
    public class NukeBoundUserInterface : BoundUserInterface
    {
        public NukeBoundUserInterface([NotNull] ClientUserInterfaceComponent owner, [NotNull] object uiKey) : base(owner, uiKey)
        {
        }
    }
}
