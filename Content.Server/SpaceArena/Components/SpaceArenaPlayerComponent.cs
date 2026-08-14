namespace Content.Server.SpaceArena.Components;

[RegisterComponent, Access(typeof(SpaceArenaMatchSystem))]
public sealed partial class SpaceArenaPlayerComponent : Component
{
    public SpaceArenaPlayerState State = SpaceArenaPlayerState.Lobby;
}

public enum SpaceArenaPlayerState : byte
{
    Lobby,
    MatchLobby,
    Preparing,
    Countdown,
    Active,
    Eliminated,
    Spectator,
    Results,
}
