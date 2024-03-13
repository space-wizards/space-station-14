namespace Content.Shared.Power;

public abstract partial class SharedPowerChargeComponent : Component
{
    /// <summary>
    /// The title used for the default charged machine window if used
    /// </summary>
    [DataField]
    public string WindowTitle { get; set; } = string.Empty;

}
