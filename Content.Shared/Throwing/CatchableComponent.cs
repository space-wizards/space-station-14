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
    /// The chance of successfully catching.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CatchChance = 1.0f;

    /// <summary>
    /// The sound to play when successfully catching.
    /// </summary>
    [DataField]
    public SoundSpecifier? CatchSuccessSound;

    /// <summary>
    /// The sound to play when failing to catch.
    /// </summary>
    [DataField]
    public SoundSpecifier? CatchFailSound;
}
