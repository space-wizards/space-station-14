using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Carries link data parsed and resolved from a TextLink <see cref="MarkupNode"/>,
/// only one field per link should be populated at a time.
/// </summary>
public sealed class TextLinkLabel : Label
{
    public string? LinkString { get; init; }
    public NetEntity? LinkEntity { get; init; }
}
