namespace Content.Server.Traitor;

/// <summary>
/// Shows a message only when examined by a traitor.
/// </summary>
[RegisterComponent]
[Access(typeof(TraitorExamineSystem))]
public sealed class TraitorExamineComponent : Component
{
    /// <summary>
    /// Message to be shown when examined.
    /// </summary>
    [DataField("message", required: true), ViewVariables(VVAccess.ReadWrite)]
    public string Message = string.Empty;
}
