namespace Content.Shared.Holiday;

/// <summary>
/// Used to inquire the server if holidays are enabled.
/// </summary>
public sealed class RequestHolidayEnabledEvent : EntityEventArgs
{
}

/// <summary>
/// Network event used to push Holiday Enabled ccvar updates from server
/// </summary>
public sealed class HolidayEnablingEvent : EntityEventArgs
{
    public bool Enabled;

    public HolidayEnablingEvent(bool enabled)
    {
        Enabled = enabled;
    }
}
