using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.Medical;

/// <summary>
/// Status effects or organs with this component will contribute a sound to the stethoscope.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StethoscopeDescriptionComponent : Component
{
    /// <summary>
    /// The message to contribute.
    /// </summary>
    [DataField(required: true)]
    public LocId Description;
}
