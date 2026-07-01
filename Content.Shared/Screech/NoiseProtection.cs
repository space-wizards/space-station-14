using Robust.Shared.GameStates;

namespace Content.Shared.Screech;

/// <summary>
/// Protects from the effects of screeches & other loud noises when worn on the HEAD, EARS or EYES slots.
/// Also protects the entity which has this component.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NoiseProtectionComponent : Component
{
    /// <summary>
    /// A quip that will be added to the description detailing its "protection from loud noises" or whatever you choose to write here.
    /// </summary>
    [DataField]
    public LocId? ExamineQuip = "screech-protection-examine-text";
}
