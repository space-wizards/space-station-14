using Content.Shared.DoAfter;

namespace Content.Server.Resist;

[RegisterComponent]
public sealed partial class CanEscapeInventoryComponent : Component
{
    /// <summary>
    /// Base doafter length for uncontested breakouts.
    /// </summary>
    [DataField("baseResistTime")]
    public float BaseResistTime = 5f;

    /// <summary>
    /// Penalty time for when you are let go during resisting.
    /// No one can pick you up during this time.
    /// </summary>
    [DataField("penaltyTimer")]
    public float PenaltyTimer = 0f;

    public bool IsEscaping => DoAfter != null;

    [DataField("doAfter")]
    public DoAfterId? DoAfter;
}
