using Robust.Shared.Audio;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Rerolls an objective's target if they cryo, requires a <see cref="PickRandomPesronComponent"/> for filtering
/// </remarks>
[RegisterComponent]
public sealed partial class RepickOnCryoComponent : Component
{
    /// <summary>
    /// Text displayed once target is rerolled
    /// </summary>
    [DataField]
    public string RerollText = "";

    /// <summary>
    /// Color for the target reroll text
    /// </summary>
    [DataField]
    public Color RerollColor = Color.OrangeRed;

    /// <summary>
    /// Sound played when the target is rerolled
    /// </summary>
    [DataField]
    public SoundSpecifier RerollSound = new SoundPathSpecifier("/Audio/Misc/cryo_warning.ogg");
}
