using Content.Shared.Radio;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server.Radio.Systems;

/// <summary>
/// This system handles tracking which players have muted radio chimes.
/// </summary>
public sealed class RadioChimeMuteSystem : EntitySystem
{
    // Track which players have muted radio chimes
    private readonly HashSet<ICommonSession> _mutedPlayers = new();

    public override void Initialize()
    {
        base.Initialize();
        
        // Subscribe to the RadioChimeMuteEvent from clients
        SubscribeNetworkEvent<RadioChimeMuteEvent>(OnRadioChimeMuteEvent);
    }

    private void OnRadioChimeMuteEvent(RadioChimeMuteEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not ICommonSession playerSession)
            return;

        if (ev.Muted)
            _mutedPlayers.Add(playerSession);
        else
            _mutedPlayers.Remove(playerSession);
    }

    /// <summary>
    /// Check if a player has muted radio chimes.
    /// </summary>
    public bool IsPlayerMuted(ICommonSession session)
    {
        return _mutedPlayers.Contains(session);
    }
}
