using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SprayPainter.AtmosPipes;

/// <summary>
/// This component describes how an entity is used to change the appearance of atmos pipes, and the state of the
/// entity's selected pipe style.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AtmosPipePainterComponent : Component
{
    /// <summary>
    /// The sound to play when painting a pipe.
    /// </summary>
    [DataField]
    public SoundSpecifier SpraySound = new SoundPathSpecifier("/Audio/Effects/spray2.ogg");

    /// <summary>
    /// The duration of the do after for painting a pipe.
    /// </summary>
    [DataField]
    public TimeSpan PipeSprayTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Pipe color chosen to spray with.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? PickedColor;

    /// <summary>
    /// Pipe colors that can be selected.
    /// </summary>
    [DataField]
    public Dictionary<string, Color> ColorPalette = new();
}
