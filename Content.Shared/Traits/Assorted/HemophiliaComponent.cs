using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This component is used for the Hemophilia Trait, it reduces the passive bleed stack reduction amount so entities with it bleed for longer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HemophiliaComponent : Component
{
    /// <summary>
    /// What multiplier should be applied to the BleedReduction when an entity bleeds?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HemophiliaBleedReductionMultiplier = 0.33f;
}
