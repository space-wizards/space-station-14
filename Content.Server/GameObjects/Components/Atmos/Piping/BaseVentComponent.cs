using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Atmos.Piping
{
    class BaseVentComponent : Component
    {
        public override string Name => "DebugVent";

        private Pipe _ventPipe;
    }
}
