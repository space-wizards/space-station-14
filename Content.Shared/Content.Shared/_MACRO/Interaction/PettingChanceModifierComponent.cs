using Content.Shared.Interaction.Components;
using Content.Shared.Whitelist;

namespace Content.Shared._MACRO.Interaction;

/// <summary>
///     Allows any entity with this component to have a different success rate when petting mobs with <see cref="InteractionPopupComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class PettingChanceModifierComponent : Component
{
    /// <summary>
    ///     Modifier of the chance to successfully pet a mob.
    /// </summary>
    [DataField]
    public float Modifier = 1;

    /// <summary>
    ///     If not null, the target must succeed the whitelist.
    /// </summary>
    [DataField]
    public EntityWhitelist? TargetWhitelist { get; set; }

    /// <summary>
    ///     If not null, the target must not pass the blacklist.
    /// </summary>
    [DataField]
    public EntityWhitelist? TargetBlacklist { get; set; }
}
