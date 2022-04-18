using Content.Shared.MachineLinking;
using Robust.Shared.GameObjects;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public sealed class TwoWayLeverComponent : Component
    {
        public TwoWayLeverState State;

        public bool NextSignalLeft;
    }
}
