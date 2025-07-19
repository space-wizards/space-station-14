using Robust.Shared.GameStates;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This component is used for the Hemophilia Trait, it reduces the passive bleed stack reduction amount so entities with it bleed for longer.
/// </summary>
[RegisterComponent]
public sealed partial class HemophiliaComponent : Component
{
    /// <summary>
    ///     What percentage should BleedReductionAmount be reduced by when an entity has the hemophilia trait?
    /// </summary>
    [DataField]
    public float HemophiliacBleedReductionAmount = 0.33f;
}
