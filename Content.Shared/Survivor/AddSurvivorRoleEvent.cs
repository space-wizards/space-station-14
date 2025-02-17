namespace Content.Shared.Survivor;

/// <summary>
/// Adds a Survivor role to the given entity
/// </summary>
[ByRefEvent]
public readonly record struct AddSurvivorRoleEvent
{
    public readonly EntityUid ToBeSurvivor;

    public AddSurvivorRoleEvent(EntityUid toBeSurvivor)
    {
        ToBeSurvivor = toBeSurvivor;
    }
}
