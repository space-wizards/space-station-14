using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Used for the lightweight trait. DrunkSystem will check for this component and modify the boozePower accordingly if it finds it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class LightweightDrunkComponent : Component
{
    [DataField("boozeMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float BoozeMultiplier = 2f;
}
