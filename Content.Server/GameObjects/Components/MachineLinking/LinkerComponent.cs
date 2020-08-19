using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.MachineLinking
{
    [RegisterComponent]
    public class LinkerComponent : Component
    {
        public override string Name => "Linker";

        [ViewVariables]
        public TransmitterComponent Link;

        public override void Initialize()
        {
            base.Initialize();

            Link = null;
        }
    }
}
