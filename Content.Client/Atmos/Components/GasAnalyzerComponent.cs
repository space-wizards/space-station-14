using Content.Shared.Atmos.Components;

namespace Content.Client.Atmos.Components
{
    [RegisterComponent]
    public sealed class GasAnalyzerComponent : SharedGasAnalyzerComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool UiUpdateNeeded;

        [ViewVariables]
        public GasAnalyzerDanger Danger;
    }
}
