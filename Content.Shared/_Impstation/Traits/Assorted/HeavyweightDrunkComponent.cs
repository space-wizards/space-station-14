using Robust.Shared.GameStates;
using Content.Shared.Drunk;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Used for the heavyweight trait. DrunkSystem will check for this component and modify the boozePower accordingly if it finds it.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDrunkSystem))]
public sealed partial class HeavyweightDrunkComponent : Component
{
    [DataField("boozeStrengthMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float BoozeStrengthMultiplier = 0.50f;
}
