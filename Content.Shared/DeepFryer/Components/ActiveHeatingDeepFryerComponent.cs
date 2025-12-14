using Robust.Shared.GameStates;

namespace Content.Shared.DeepFryer.Components;

/// <summary>
/// Used to track deep fryers that are actively heating the oil in their vat.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveHeatingDeepFryerComponent : Component;
