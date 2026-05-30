using Content.Shared.Revolutionary;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mindshield.Components;

/// <summary>
/// This component, on a clothing item, on an implant or on an entity, prevents "mind control". This means that you won't be convertable to the revolution, for instance.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRevolutionarySystem))]
public sealed partial class MindShieldComponent : Component;
