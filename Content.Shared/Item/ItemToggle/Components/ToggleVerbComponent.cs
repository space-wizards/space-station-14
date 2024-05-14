using Content.Shared.Item.ItemToggle;
using Robust.Shared.GameStates;

namespace Content.Shared.Item.ItemToggle.Components;

/// <summary>
/// Adds a verb for toggling something, requires <see cref="ItemToggleComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ToggleVerbSystem))]
public sealed partial class ToggleVerbComponent : Component
{
    /// <summary>
    /// Text the verb will have.
    /// Gets passed "entity" as the entity's identity string.
    /// </summary>
    [DataField(required: true)]
    public LocId Text = string.Empty;
}
