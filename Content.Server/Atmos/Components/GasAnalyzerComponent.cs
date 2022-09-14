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
        // Set to a tiny bit after the default because otherwise the user often gets a blank window when first using
        public float AccumulatedFrametime = 2.01f;

        /// <summary>
        /// How often to update the analyzer
        /// </summary>
        public float UpdateInterval = 1f;
    }
}
