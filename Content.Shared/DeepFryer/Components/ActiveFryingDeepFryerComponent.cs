using Robust.Shared.GameStates;

namespace Content.Shared.DeepFryer.Components;

/// <summary>
/// Used to track deep fryers that are in the act of frying something.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveFryingDeepFryerComponent : Component;
