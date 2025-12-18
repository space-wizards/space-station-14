using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Scrambles the entity's identity and DNA, turning them into a randomized humanoid of the same species.
/// If TargetUser is true the user will be scrambled instead.
/// Used for dna scrambler implants.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DnaScrambleOnTriggerComponent : BaseXOnTriggerComponent;
