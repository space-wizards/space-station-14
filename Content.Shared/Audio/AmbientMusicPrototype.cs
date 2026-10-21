using Content.Shared.EntityConditions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Audio;

/// <summary>
/// Attaches a rules prototype to sound files to play ambience.
/// </summary>
[Prototype]
public sealed partial class AmbientMusicPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// Traditionally you'd prioritise most rules to least as priority but in our case we'll just be explicit.
    /// </summary>
    [DataField]
    public int Priority = 0;

    /// <summary>
    /// Can we interrupt this ambience for a better prototype if possible?
    /// </summary>
    [DataField]
    public bool Interruptable = false;

    //Whether this ambience is allowed to play twice in a row
    [DataField]
    public bool AllowRepeat = true;

    /// <summary>
    /// Do we fade-in. Useful for songs.
    /// </summary>
    [DataField]
    public bool FadeIn;

    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    [DataField]
    public EntityCondition[]? Conditions;
}
