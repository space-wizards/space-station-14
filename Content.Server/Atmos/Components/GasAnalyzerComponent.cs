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

    /// <summary>
    /// Used to keep track of which analyzers are active for update purposes
    /// </summary>
    [RegisterComponent]
    public sealed class ActiveGasAnalyzerComponent : Component
    {
        public float AccumulatedFrametime;

        /// <summary>
        /// How often to update the analyzer
        /// </summary>
        public float UpdateInterval = 1f;
    }
}
