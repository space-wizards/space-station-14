using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This component is used for the Hemophilia Trait, it reduces the passive bleed stack reduction amount so entities with it bleed for longer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HemophiliaStatusEffectComponent : Component
{
    /// <summary>
    /// Multiplier to use for the amount of bloodloss reduction during a bleed tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BleedReductionMultiplier = 0.33f;

    /// <summary>
    /// Multiplier to use for the amount of blood lost during a bleed tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BleedAmountMultiplier = 1f;
}
