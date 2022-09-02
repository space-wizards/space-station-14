using Content.Shared.Atmos.Components;
using Robust.Shared.Map;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedGasAnalyzerComponent))]
    public sealed class GasAnalyzerComponent : SharedGasAnalyzerComponent
    {
        [ViewVariables] public EntityUid? Target;
        [ViewVariables] public EntityUid User;
        [ViewVariables] public EntityCoordinates? LastPosition;
        [ViewVariables] public bool Enabled;
    }
}
