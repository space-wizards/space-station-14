namespace Content.Server.HyperLink;

[RegisterComponent]
public sealed partial class HyperLinkComponent : Component
{
    [DataField]
    public string UrlType = string.Empty;
}
