namespace Content.Server.Dragon.Events;


[ByRefEvent]
public readonly struct DragonRiftFinishedEvent
{
    public readonly EntityUid Dragon;

    public DragonRiftFinishedEvent(EntityUid dragon)
    {
        Dragon = dragon;
    }
}
