using Robust.Shared.GameStates;

namespace Content.Shared.Cloning;

/// <summary>
/// Shrimply a tracking component for pods that are cloning.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveCloningPodComponent : Component;
