using Robust.Shared.GameStates;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This component is used for the Hemophilia Trait, it reduces the passive bleed stack reduction amount so entities with it bleed for longer.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HemophiliaComponent : Component
{
    /// <summary>
    ///     How much should bleeding be reduced every update interval for the hemophilia trait?
    /// </summary>
    [DataField]
    public float HemophiliacBleedReductionAmount = 0.10f;
}
