using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Content.Shared.Eye;

/// <summary>
/// Applies visibility layer changes while the owning status effect is active.
/// Use only on status effect entities.
/// </summary>
[RegisterComponent]
public sealed partial class VisibilityModifierStatusComponent : Component
{
    /// <summary>
    /// Visibility layers added while the effect is active.
    /// </summary>
    [DataField]
    public List<VisibilityFlags> AddVisibility = new();

    /// <summary>
    /// Visibility layers removed while the effect is active.
    /// </summary>
    [DataField]
    public List<VisibilityFlags> RemoveVisibility = new();
}
