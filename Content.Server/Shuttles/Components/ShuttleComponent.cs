using Content.Shared.Shuttles;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent]
    public sealed class ShuttleComponent : SharedShuttleComponent
    {
        [ViewVariables]
        public readonly float[] LinearThrusters = new float[4];

        [ViewVariables]
        public float AngularThrust = 0f;
    }
}
