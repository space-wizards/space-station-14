using Content.Server.Chat.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Maths;

namespace Content.Server.Chat;

/// <summary>
/// Dispatches an announcement to everyone when the entity is mapinit'd.
/// </summary>
[RegisterComponent, Access(typeof(AnnounceOnSpawnSystem))]
public sealed partial class AnnounceOnSpawnComponent : Component
{
    /// <summary>
    /// Locale id of the announcement message.
    /// </summary>
    [DataField(required: true)]
    public LocId Message = string.Empty;

    /// <summary>
    /// Locale id of the announcement's sender, defaults to Central Command.
    /// </summary>
    [DataField]
    public LocId? Sender;

    /// <summary>
    /// Sound override for the announcement.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Color override for the announcement.
    /// </summary>
    [DataField]
    public Color? Color;
}
