
namespace Content.Server.Objectives.Components;

/// <summary>
///     Objective that requires you to hold a document until evacuation. When assigned, will give the player the document.
/// </summary>
[RegisterComponent]
public sealed partial class HoldDocumentObjectiveComponent : Component
{
    /// <summary>
    ///     Can the document be traded? Will be set to false if there is a trade objective paired with the hold objecive.
    ///     Can also just be set to false by default if the objective shouldn't be traded in the first place.
    /// </summary>
    [DataField]
    public bool IsAvailable = true;
    [DataField(required: true)]
    public string Title;
    [DataField(required: true)]
    public string Description;
}
