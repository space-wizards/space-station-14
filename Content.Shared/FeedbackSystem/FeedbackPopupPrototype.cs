using Robust.Shared.Prototypes;

namespace Content.Shared.FeedbackSystem;

/// <summary>
/// Prototype that describes the contents of a feedback popup.
/// </summary>
[Prototype]
public sealed partial class FeedbackPopupPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// What server the popup is from, you must edit the ccvar to include this for the popup to appear!
    /// </summary>
    [DataField(required: true)]
    public string PopupOrigin = "";

    /// <summary>
    /// Title of the popup. This supports rich text so you can use colors and stuff.
    /// </summary>
    [DataField(required: true)]
    public string Title = "";

    /// <summary>
    /// List of "paragraphs" that are placed in the middle of the popup. Put any relevant information about what to give
    /// feedback on here! [bold]Rich text is allowed[/bold]
    /// </summary>
    [DataField(required: true)]
    public string Description = "";

    /// <summary>
    /// The kind of response the player should expect to give; good examples are "Survey", "Discord Channel", "Feedback Thread" etc.
    /// Will be listed near the "Open Link" button; rich text is not allowed.
    /// </summary>
    [DataField]
    public string ResponseType = "";

    /// <summary>
    /// A link leading to where you want players to give feedback. Discord channel, form etc...
    /// </summary>
    [DataField]
    public string? ResponseLink;

    /// <summary>
    /// Should this feedback be shown when the round ends.
    /// </summary>
    [DataField]
    public bool ShowRoundEnd = true;
}
