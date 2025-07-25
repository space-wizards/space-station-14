using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

/// <summary>
/// Prevents the attached entity from taking FTL.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NoFTLComponent : Component
{

}
