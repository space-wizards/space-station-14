using Content.Shared.Revolutionary;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mindshield.Components;

/// <summary>
/// Command members get this component to prevent other mindshields being applied to them (they already have a better one)
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRevolutionarySystem))]
public sealed partial class CommandMindShieldComponent : Component
{

}
