using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Item.ItemToggle.Components;

/// <summary>
/// Handles the active sound being played continuously with some items that are activated (ie e-sword hum).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemToggleActiveSoundComponent : Component
{
    /// <summary>
    ///     The continuous noise this item makes when it's activated (like an e-sword's hum).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public SoundSpecifier? ActiveSound;

    /// <summary>
    ///     Used when the item emits sound while active.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public EntityUid? PlayingStream;
}
