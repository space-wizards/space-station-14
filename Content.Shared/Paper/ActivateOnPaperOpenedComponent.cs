using Content.Shared.Paper;

namespace Content.Shared.Paper;

/// <summary>
/// Activates the item when used to write on paper, as if Z was pressed.
/// </summary>
[RegisterComponent]
[Access(typeof(PaperSystem))]
public sealed partial class ActivateOnPaperOpenedComponent : Component
{
}
