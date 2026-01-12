using Robust.Shared.GameStates;

namespace Content.Shared.Access.Components;

/// <summary>
/// Allows an ID card to copy accesses from other IDs and to change the name, job title and job icon via an interface.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AgentIDCardComponent : Component;
