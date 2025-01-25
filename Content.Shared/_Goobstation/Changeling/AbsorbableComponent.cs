using Robust.Shared.GameStates;

namespace Content.Shared.Changeling;

/// <summary>
///     Component that indicates that a person can be absorbed by a changeling.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AbsorbableComponent : Component
{
    [DataField("disabled")]
    public bool Disabled = false;

    [DataField("reducedBiomass")]
    public bool ReducedBiomass = false;
}
