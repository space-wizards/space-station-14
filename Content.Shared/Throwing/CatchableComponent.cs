using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Throwing;

/// <summary>
/// Allows this entity to be caught in your hands when someone else throws it at you.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CatchableComponent : Component
{
    /// <summary>
    /// If true this item can only be caught while in combat mode.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequireCombatMode;

    /// <summary>
    /// The chance of successfully catching.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CatchChance = 1.0f;

    /// <summary>
    /// Optional whitelist for who can catch this item.
    /// </summary>
    /// <summary>
    /// Example usecase: Only someone who knows martial arts can catch grenades.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? CatcherWhitelist;

    /// <summary>
    /// The sound to play when successfully catching.
    /// </summary>
    [DataField]
    public SoundSpecifier? CatchSuccessSound;
}
