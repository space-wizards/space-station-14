namespace Content.Shared.Cuffs.Components;
public abstract class SharedLegcuffComponent : Component
{
    /// <summary>
    /// How long it should take to put these legcuffs onto an entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("cuffTime")]
    public float CuffTime = 6f;

    /// <summary>
    /// How long it should take to remove someone else's legcuffs.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("uncuffTime")]
    public float UncuffTime = 6f;

    /// <summary>
    /// How long it should take for an entity to remove their own legcuffs.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("breakoutTime")]
    public float BreakoutTime = 10f;

    /// <summary>
    /// Should these legcuffs ensnare someone on step?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("canStepTrigger")]
    public bool CanStepTrigger;

    /// <summary>
    /// Should these legcuffs ensnare someone when thrown?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("canThrowTrigger")]
    public bool CanThrowTrigger;
}

public sealed class LegcuffAttemptEvent : CancellableEntityEventArgs
{
    //TODO: Legcuff targeting logic here
    //Might be better off changed to an uncuff event
}

public sealed class LegcuffChangeEvent : EventArgs
{
    //TODO: Legcuff change logic here
    //You might not even need any logic in here.
}
