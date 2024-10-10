using Content.Shared.Substation;

namespace Content.Server.Power.Substation;

[RegisterComponent, Access(typeof(SubstationVisualizerSystem))]
public sealed partial class SubstationVisualizerComponent : Component
{
    [ViewVariables]
    public SubstationChargeState LastChargeState;
    [ViewVariables]
    public TimeSpan LastChargeStateTime;
    [ViewVariables]
    public TimeSpan VisualsChangeDelay = TimeSpan.FromSeconds(1);
}