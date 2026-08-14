namespace Content.Shared.SpaceArena;

public enum SpaceArenaMatchState : byte
{
    Waiting,
    Preparing,
    Countdown,
    Active,
    Ending,
    Finished,
    Cleanup,
}
