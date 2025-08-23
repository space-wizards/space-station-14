using Robust.Shared.GameStates;

namespace Content.Server.Starlight.Cybernetics.Components;

/// <summary>
/// This component prevents entity from tasting
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class UnableToTasteComponent : Component
{
}
