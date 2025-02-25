namespace Content.Shared.Attachments.Components;

[RegisterComponent]
public sealed partial class AttachmentComponent : Component
{
    /// <summary>
    ///     If we should add components even if they already exist.
    /// </summary>
    [ViewVariables, DataField]
    public bool ForceComponents = true;
}
