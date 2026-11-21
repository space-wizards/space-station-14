using Robust.Shared.GameStates;

namespace Content.Shared.Cloning.Components;

/// <summary>
/// Shrimply a tracking component for pods that are cloning.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveCloningPodComponent : Component;
