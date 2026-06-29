using Robust.Shared.GameStates;

namespace Content.Shared.Mindshield.Components;

/// <summary>
/// This component, on a clothing item, on an implant or on an entity, prevents "mind control". This means that you won't be convertable to the revolution, for instance.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedMindShieldSystem))]
public sealed partial class MindShieldComponent : Component;
