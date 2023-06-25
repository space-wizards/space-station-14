namespace Content.Server.Fugitive;

[RegisterComponent]
public sealed class FugitiveComponent: Component
{
    /// <summary>
    ///     Whether or not a wanted notice has been sent to the stations
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool WantedNoticeSent { get; set; } = false;
}
