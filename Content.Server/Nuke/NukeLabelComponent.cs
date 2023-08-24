namespace Content.Server.Nuke;

/// <summary>
///     This generates a label for a nuclear bomb.
/// </summary>
/// <remarks>
///     This is a separate component because the fake nuclear bomb keg exists.
/// </remarks>
[RegisterComponent]
public sealed partial class NukeLabelComponent : Component
{
    [DataField("prefix")] public string NukeLabel = "nuke-label-nanotrasen";
    [DataField("serialLength")] public int SerialLength = 6;
}
