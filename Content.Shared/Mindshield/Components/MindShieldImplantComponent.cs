using Content.Shared.Revolutionary;
using Robust.Shared.GameStates;

namespace Content.Shared.Mindshield.Components;

/// <summary>
/// Component given to an entity to mark it is a mindshield implant that will unconvert revolutionaries when implanted.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRevolutionarySystem))]
public sealed partial class MindShieldImplantComponent : Component;
