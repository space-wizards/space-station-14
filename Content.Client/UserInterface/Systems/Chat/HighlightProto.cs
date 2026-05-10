namespace Content.Client.UserInterface.Systems.Chat;

/// <summary>
/// The prototype for an array of chat highlights
/// </summary>
[DataDefinition]
public sealed partial class HighlightProto
{
    /// <summary>
    /// A string holding the Server ID, used to index highlights per-server
    /// </summary>
    [DataField] public string ServerId;

    /// <summary>
    /// An array of highlight strings
    /// </summary>
    [DataField] public string[] Highlights;
}
