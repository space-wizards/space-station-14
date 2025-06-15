using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SprayPainter.Airlocks.Components;

/// <summary>
/// This component describes how an entity is used to change the appearance of airlocks, and the state of the entity's
/// selected airlock style.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AirlockPainterComponent : Component
{
    /// <summary>
    /// The sound to play when the airlock style is changed.
    /// </summary>
    [DataField]
    public SoundSpecifier SpraySound = new SoundPathSpecifier("/Audio/Effects/spray2.ogg");

    /// <summary>
    /// The duration of the do after for using this entity to change the style of the airlock.
    /// </summary>
    [DataField]
    public TimeSpan AirlockSprayTime = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Airlock style index selected.
    /// After prototype reload this might not be the same style but it will never be out of bounds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Index;
}
