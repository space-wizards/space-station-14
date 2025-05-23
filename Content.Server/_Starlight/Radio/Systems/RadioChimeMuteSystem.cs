// This file is intentionally left empty as the radio chime system has been moved to the client side.
// The file is kept to maintain compatibility with existing code references.
// The actual implementation is now in Content.Client.Audio.RadioChimeMuteSystem.

using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server.Radio.Systems;

/// <summary>
/// This system is a placeholder for the client-side RadioChimeMuteSystem.
/// Radio chimes are now handled entirely on the client side.
/// </summary>
public sealed class RadioChimeMuteSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// This method is kept for compatibility but no longer does anything.
    /// Radio chime muting is now handled client-side.
    /// </summary>
    public bool IsPlayerMuted(ICommonSession session)
    {
        return false;
    }
}
