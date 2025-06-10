using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This component is used for the Hemophilia Trait, it reduces the passive bleed stack reduction amount so entities with it bleed for longer.
/// The reduction SHOULD be located in BloodstreamComponent.cs as HemophiliacBleedReductionAmount
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HemophiliaComponent : Component;
