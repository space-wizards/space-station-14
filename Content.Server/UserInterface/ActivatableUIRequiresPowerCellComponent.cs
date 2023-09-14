using Content.Server.PowerCell;
using Content.Shared.PowerCell;
using Content.Shared.UserInterface;

namespace Content.Server.UserInterface;

/// <summary>
/// Specifies that the attached entity requires <see cref="PowerCellDrawComponent"/> power.
/// </summary>
[RegisterComponent]
public sealed partial class ActivatableUIRequiresPowerCellComponent : Component
{

}
