using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Audio.Jukebox;

/// <summary>
/// Soundtrack that's visible on the jukebox list.
/// </summary>
[Prototype]
public sealed class JukeboxPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    /// <summary>
    /// User friendly name to use in UI.
    /// </summary>
    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public ResPath Path;
}
