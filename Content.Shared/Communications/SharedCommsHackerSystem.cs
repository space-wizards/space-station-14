namespace Content.Shared.Communications;

/// <summary>
/// Only exists in shared to provide API and for access.
/// All logic is serverside.
/// </summary>
public abstract class SharedCommsHackerSystem : EntitySystem
{
    /// <summary>
    /// Set the list of threats to choose from when hacking a comms console.
    /// </summary>
    public void SetThreats(EntityUid uid, List<Threat> threats, CommsHackerComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Threats = threats;
    }
}
