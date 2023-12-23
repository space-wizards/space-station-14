using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Item;

/// <summary>
/// Handles the active sound being played continuously with some items that are activated (ie e-sword hum).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ItemToggleActiveSoundComponent : Component
{
    /// <summary>
    ///     The continuous noise this item makes when it's activated (like an e-sword's hum). This loops.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public SoundSpecifier? ActiveSound;

    /// <summary>
    ///     Used when the item emits sound while active.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public EntityUid? PlayingStream;
}

/// <summary>
/// Raised in order to effect changes upon the ActiveSound of the entity.
/// </summary>
[ByRefEvent]
public record struct ItemToggleActiveSoundUpdateEvent(bool Activated, bool Predicted, EntityUid? User)
{
    public bool Activated = Activated;
    public bool Predicted = Predicted;
    public EntityUid? User = User;
}
